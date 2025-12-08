using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using PalpitheionApi.Services;

namespace PalpitheionApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public record LoginRequest(string UserName, string Password);
    public record RegisterRequest(string UserName, string Password);
    public record AuthResponse(string Token, string UserName, IEnumerable<string> Roles);

    /// <summary>
    /// POST auth/entrar
    /// Faz login com usuário e senha e retorna token JWT se bem sucedido.
    /// </summary>
    [HttpPost("entrar")]
    public async Task<IActionResult> Entrar([FromBody] LoginRequest model)
    {
        if (model is null) return BadRequest("Requisição inválida.");

        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user == null)
            return Unauthorized("Usuário ou senha inválidos.");

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
            return Unauthorized("Usuário ou senha inválidos.");

        var token = await _jwtService.GenerateTokenAsync(user, _userManager);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponse(token, user.UserName ?? string.Empty, roles));
    }

    /// <summary>
    /// POST auth/cadastrar
    /// Cria um novo usuário (role padrão: "Usuario") e retorna token JWT.
    /// </summary>
    [HttpPost("cadastrar")]
    public async Task<IActionResult> Cadastrar([FromBody] RegisterRequest model)
    {
        if (model is null) return BadRequest("Requisição inválida.");

        // Verifica se usuário já existe
        var existing = await _userManager.FindByNameAsync(model.UserName);
        if (existing != null)
            return Conflict("Nome de usuário já está em uso.");

        var user = new IdentityUser
        {
            UserName = model.UserName,
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Falha ao criar usuário {UserName}: {Errors}", model.UserName, errors);
            return BadRequest(new { Errors = createResult.Errors.Select(e => e.Description) });
        }

        // Adiciona role padrão "Usuario" — espera-se que role tenha sido criada no seed.
        var addRoleResult = await _userManager.AddToRoleAsync(user, "Usuario");
        if (!addRoleResult.Succeeded)
        {
            _logger.LogWarning("Falha ao adicionar role ao usuário {UserName}: {Errors}", model.UserName,
                string.Join("; ", addRoleResult.Errors.Select(e => e.Description)));
            // Não falha o cadastro por causa da role — apenas reporta
        }

        var token = await _jwtService.GenerateTokenAsync(user, _userManager);
        var roles = await _userManager.GetRolesAsync(user);

        return Created(string.Empty, new AuthResponse(token, user.UserName ?? string.Empty, roles));
    }
}