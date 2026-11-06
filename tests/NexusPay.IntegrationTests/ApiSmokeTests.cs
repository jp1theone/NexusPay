using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NexusPay.IntegrationTests;

public sealed class ApiSmokeTests(NexusPayApiFactory factory) : IClassFixture<NexusPayApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ProtectedRoutes_RejectAnonymousRequests()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_IsCaseInsensitive_AndTokenCanReadProfile()
    {
        using var client = factory.CreateClient();
        var login = await LoginAsync(client, factory.AdminEmail.ToUpperInvariant(), factory.AdminPassword);

        Assert.Equal("Bearer", login.TokenType);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var profileResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        using var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        Assert.Contains("Administrator", profile.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }

    [Fact]
    public async Task CustomersAndOperations_PersistAndHonorIdempotency()
    {
        using var client = factory.CreateClient();
        var login = await LoginAsync(client, factory.AdminEmail, factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var unique = Guid.NewGuid().ToString("N")[..12];
        var customerResponse = await client.PostAsJsonAsync("/api/customers", new
        {
            customerCode = $"IT-{unique}", firstName = "Ada", lastName = "Lovelace",
            email = $"{unique}@example.test", phone = (string?)null,
            documentType = (string?)null, documentNumber = (string?)null
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Add("Idempotency-Key", $"create-{unique}");
        var operationResponse = await client.PostAsJsonAsync("/api/operations", new
        {
            customerId, type = "Payment", amount = 25.50m, currency = "pen", description = "integration smoke test"
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, operationResponse.StatusCode);
        using var operationJson = JsonDocument.Parse(await operationResponse.Content.ReadAsStringAsync());
        var operationId = operationJson.RootElement.GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");

        client.DefaultRequestHeaders.Add("Idempotency-Key", $"create-{unique}");
        var repeatedCreate = await client.PostAsJsonAsync("/api/operations", new
        {
            customerId, type = "Payment", amount = 25.50m, currency = "PEN", description = "integration smoke test"
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, repeatedCreate.StatusCode);
        using (var repeatedJson = JsonDocument.Parse(await repeatedCreate.Content.ReadAsStringAsync()))
            Assert.Equal(operationId, repeatedJson.RootElement.GetProperty("id").GetGuid());
        client.DefaultRequestHeaders.Remove("Idempotency-Key");

        client.DefaultRequestHeaders.Add("Idempotency-Key", $"process-{unique}");
        var processResponse = await client.PostAsync($"/api/operations/{operationId}/process", null);
        Assert.True(processResponse.StatusCode == HttpStatusCode.OK,
            $"Expected operation processing to succeed but got {(int)processResponse.StatusCode}: {await processResponse.Content.ReadAsStringAsync()}");
        using var processedJson = JsonDocument.Parse(await processResponse.Content.ReadAsStringAsync());
        Assert.Equal("Completed", processedJson.RootElement.GetProperty("status").GetString());
        client.DefaultRequestHeaders.Remove("Idempotency-Key");

        client.DefaultRequestHeaders.Add("Idempotency-Key", $"process-{unique}");
        var repeatedProcess = await client.PostAsync($"/api/operations/{operationId}/process", null);
        Assert.Equal(HttpStatusCode.OK, repeatedProcess.StatusCode);
        using (var repeatedProcessJson = JsonDocument.Parse(await repeatedProcess.Content.ReadAsStringAsync()))
            Assert.Equal("Completed", repeatedProcessJson.RootElement.GetProperty("status").GetString());
        client.DefaultRequestHeaders.Remove("Idempotency-Key");

        var detailResponse = await client.GetAsync($"/api/operations/{operationId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detailJson = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, detailJson.RootElement.GetProperty("paymentAttempts").GetArrayLength());
        Assert.Equal(3, detailJson.RootElement.GetProperty("statusHistory").GetArrayLength());

        var rangeStart = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-2).ToString("O"));
        var rangeEnd = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));
        var providersResponse = await client.GetAsync("/api/providers");
        Assert.Equal(HttpStatusCode.OK, providersResponse.StatusCode);
        var dashboardResponse = await client.GetAsync($"/api/dashboard/summary?from={rangeStart}&to={rangeEnd}");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        using var dashboardJson = JsonDocument.Parse(await dashboardResponse.Content.ReadAsStringAsync());
        Assert.True(dashboardJson.RootElement.GetProperty("completedOperations").GetInt32() >= 1);
        Assert.Contains(dashboardJson.RootElement.GetProperty("simulatedVolumeByCurrency").EnumerateArray(),
            item => item.GetProperty("currency").GetString() == "PEN");
        var auditResponse = await client.GetAsync($"/api/audit-logs?from={rangeStart}&to={rangeEnd}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        using var auditJson = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        Assert.True(auditJson.RootElement.GetProperty("totalCount").GetInt32() >= 2);
        var integrationLogsResponse = await client.GetAsync($"/api/integration-logs?from={rangeStart}&to={rangeEnd}");
        Assert.Equal(HttpStatusCode.OK, integrationLogsResponse.StatusCode);
        using var integrationJson = JsonDocument.Parse(await integrationLogsResponse.Content.ReadAsStringAsync());
        Assert.True(integrationJson.RootElement.GetProperty("totalCount").GetInt32() >= 1);
        var customerReport = await client.GetAsync($"/api/reports/customers?from={rangeStart}&to={rangeEnd}&pageSize=100");
        Assert.True(customerReport.StatusCode == HttpStatusCode.OK,
            $"Customer report failed with {(int)customerReport.StatusCode}: {await customerReport.Content.ReadAsStringAsync()}");
        using var customersReportJson = JsonDocument.Parse(await customerReport.Content.ReadAsStringAsync());
        var customerReportCount = customersReportJson.RootElement.GetProperty("totalCount").GetInt32();
        Assert.True(customerReportCount >= 1);
        var customerFound = ContainsCreatedCustomer(customersReportJson.RootElement, $"IT-{unique}");
        for (var page = 2; !customerFound && page <= (customerReportCount + 99) / 100; page++)
        {
            using var customerPage = JsonDocument.Parse(await client.GetStringAsync(
                $"/api/reports/customers?from={rangeStart}&to={rangeEnd}&page={page}&pageSize=100"));
            customerFound = ContainsCreatedCustomer(customerPage.RootElement, $"IT-{unique}");
        }
        Assert.True(customerFound, $"Customer report did not include IT-{unique}; totalCount={customerReportCount}; firstPage={customersReportJson.RootElement.GetProperty("items").GetRawText()}");
        var providerReport = await client.GetAsync($"/api/reports/providers?from={rangeStart}&to={rangeEnd}");
        Assert.True(providerReport.StatusCode == HttpStatusCode.OK,
            $"Provider report failed with {(int)providerReport.StatusCode}: {await providerReport.Content.ReadAsStringAsync()}");
        using var providersReportJson = JsonDocument.Parse(await providerReport.Content.ReadAsStringAsync());
        Assert.True(providersReportJson.RootElement.GetProperty("totalCount").GetInt32() >= 1);
        var csvResponse = await client.GetAsync($"/api/reports/operations.csv?from={rangeStart}&to={rangeEnd}");
        Assert.True(csvResponse.StatusCode == HttpStatusCode.OK,
            $"CSV report failed with {(int)csvResponse.StatusCode}: {await csvResponse.Content.ReadAsStringAsync()}");
        Assert.Contains("OperationCode,CustomerCode", await csvResponse.Content.ReadAsStringAsync());
        var customerCsv = await client.GetAsync($"/api/reports/customers.csv?from={rangeStart}&to={rangeEnd}");
        Assert.Equal(HttpStatusCode.OK, customerCsv.StatusCode);
        Assert.Contains("CustomerCode,CustomerName,Currency", await customerCsv.Content.ReadAsStringAsync());
        var providerCsv = await client.GetAsync($"/api/reports/providers.csv?from={rangeStart}&to={rangeEnd}");
        Assert.Equal(HttpStatusCode.OK, providerCsv.StatusCode);
        Assert.Contains("ProviderCode,TotalAttempts", await providerCsv.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Administrator_CanCreateAndConfigureSimulatedProvider()
    {
        using var client = factory.CreateClient();
        var login = await LoginAsync(client, factory.AdminEmail, factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var code = $"IT-{Guid.NewGuid():N}"[..15];
        var create = await client.PostAsJsonAsync("/api/providers", new
        {
            providerCode = code, name = "Integration Simulator"
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        var update = await client.PutAsJsonAsync($"/api/providers/{id}", new
        {
            name = "Integration Simulator Updated", isEnabled = false, isAvailable = true
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var configured = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        Assert.False(configured.RootElement.GetProperty("isEnabled").GetBoolean());
        Assert.False(configured.RootElement.GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public async Task Auditor_CanReadButCannotWriteOperationalData()
    {
        using var adminClient = factory.CreateClient();
        var admin = await LoginAsync(adminClient, factory.AdminEmail, factory.AdminPassword);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"auditor-{suffix}@example.test";
        var password = $"NexusPay-{suffix}!Aa1";
        var createdUser = await adminClient.PostAsJsonAsync("/api/users", new
        {
            email, password, roles = new[] { "Auditor" }
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createdUser.StatusCode);

        using var auditorClient = factory.CreateClient();
        var auditor = await LoginAsync(auditorClient, email, password);
        auditorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auditor.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await auditorClient.GetAsync("/api/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await auditorClient.GetAsync("/api/operations")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await auditorClient.GetAsync("/api/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await auditorClient.PostAsJsonAsync("/api/customers", new
        {
            customerCode = $"AUD-{suffix}", firstName = "Test", lastName = "Auditor",
            email = $"{suffix}@example.test", phone = (string?)null, documentType = (string?)null, documentNumber = (string?)null
        }, JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await auditorClient.PostAsJsonAsync("/api/providers", new
        {
            providerCode = $"AUD-{suffix}", name = "Unauthorized Provider"
        }, JsonOptions)).StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_CanChangePasswordAndOldPasswordStopsWorking()
    {
        using var adminClient = factory.CreateClient();
        var admin = await LoginAsync(adminClient, factory.AdminEmail, factory.AdminPassword);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"password-{suffix}@example.test";
        var oldPassword = $"NexusPay-{suffix}!Aa1";
        var newPassword = $"NexusPay-Changed-{suffix}!Aa1";
        var createdUser = await adminClient.PostAsJsonAsync("/api/users", new
        {
            email, password = oldPassword, roles = new[] { "Operator" }
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createdUser.StatusCode);

        using var userClient = factory.CreateClient();
        var login = await LoginAsync(userClient, email, oldPassword);
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var changed = await userClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = oldPassword, newPassword
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        var oldLogin = await userClient.PostAsJsonAsync("/api/auth/login", new { email, password = oldPassword }, JsonOptions);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        var newLogin = await userClient.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task LastActiveAdministrator_CannotBeDemotedOrDisabled()
    {
        using var client = factory.CreateClient();
        var login = await LoginAsync(client, factory.AdminEmail, factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/auth/me", JsonOptions);
        var adminId = profile.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(adminId));

        var demote = await client.PutAsJsonAsync($"/api/users/{adminId}/roles", new { roles = new[] { "Operator" } }, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
        var disable = await client.PatchAsJsonAsync($"/api/users/{adminId}/status", new { isActive = false }, JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, disable.StatusCode);
    }

    private static async Task<LoginToken> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password }, JsonOptions);
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Login failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<LoginToken>(JsonOptions)
            ?? throw new InvalidOperationException("The login response was empty.");
    }

    private static bool ContainsCreatedCustomer(JsonElement report, string customerCode) =>
        report.GetProperty("items").EnumerateArray().Any(item =>
            string.Equals(item.GetProperty("customerCode").GetString(), customerCode, StringComparison.OrdinalIgnoreCase) &&
            item.GetProperty("currency").GetString() == "PEN");

    private sealed record LoginToken(string AccessToken, string TokenType);
}
