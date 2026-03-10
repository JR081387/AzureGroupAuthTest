using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;

namespace AzureGroupAuth.Services
{
    public class GraphService
    {
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient _graphClient;

        public GraphService(IConfiguration configuration)
        {
            _configuration = configuration;

            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var tenantId = _configuration["AzureAd:TenantId"];

            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret);

            _graphClient = new GraphServiceClient(clientSecretCredential);
        }

        public async Task<List<GroupMember>> GetGroupMembersAsync(string groupId)
        {
            var members = new List<GroupMember>();

            try
            {
                var groupMembers = await _graphClient.Groups[groupId].Members
                    .GetAsync();

                if (groupMembers?.Value != null)
                {
                    foreach (var member in groupMembers.Value)
                    {
                        if (member is Microsoft.Graph.Models.User user)
                        {
                            members.Add(new GroupMember
                            {
                                DisplayName = user.DisplayName ?? "N/A",
                                Email = user.Mail ?? user.UserPrincipalName ?? "N/A",
                                Id = user.Id ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching group members: {ex.Message}");
            }

            return members;
        }

        public async Task<Dictionary<string, List<GroupMember>>> GetAllGroupMembersAsync()
        {
            var adminGroupId = _configuration["AzureAd:Groups:Admins"];
            var usersEditGroupId = _configuration["AzureAd:Groups:UsersEdit"];
            var usersViewGroupId = _configuration["AzureAd:Groups:UsersView"];

            var result = new Dictionary<string, List<GroupMember>>();

            if (!string.IsNullOrEmpty(adminGroupId))
                result["Admins"] = await GetGroupMembersAsync(adminGroupId);

            if (!string.IsNullOrEmpty(usersEditGroupId))
                result["Users Edit"] = await GetGroupMembersAsync(usersEditGroupId);

            if (!string.IsNullOrEmpty(usersViewGroupId))
                result["Users View"] = await GetGroupMembersAsync(usersViewGroupId);

            return result;
        }
    }

    public class GroupMember
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }
}
