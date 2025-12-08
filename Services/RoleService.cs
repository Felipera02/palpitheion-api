using Microsoft.AspNetCore.Identity;

namespace PalpitheionApi.Services;

public class RoleService(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager)
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    public async Task SeedRoleAsync()
    {
        string[] roles = { "Admin", "Usuario" };

        foreach (var role in roles)
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
    }

    public async Task SeedAdminUserAsync(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            throw new ArgumentException("Username ou password não podem ser nulos ou vazios");

        var adminUser = await _userManager.FindByNameAsync(userName);

        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = userName
            };
            var result = await _userManager.CreateAsync(adminUser, password);

            if (!result.Succeeded)
                throw new Exception("Falha ao criar usuário admin: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
            await _userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
