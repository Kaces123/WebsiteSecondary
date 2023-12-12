using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using O9d.AspNet.FluentValidation;
using WebApplication1.Authentication;
using WebApplication1.Authentication.Model;
using WebApplication1.Data;
using WebApplication1.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication1.Authentication.Model;
using WebApplication1.Authentication;
using WebApplication1.Data;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient<JwtTokenService>();
builder.Services.AddScoped<AuthDbSeeder>();



builder.Services.AddIdentity<ForumRestUser, IdentityRole>()
    .AddEntityFrameworkStores<ShopDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]));
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

/*
 /api/v1/shops GET List 200
 /api/v1/shops/{id} GET One 200
 /api/v1/shops POST Create 201
 /api/v1/shops/{id} PUT/PATCH Modify 200
 /api/v1/shops/{id} DELETE Remove 200/204
 */

/*
 * Shops
 */

var shopsGroup = app.MapGroup("/api").WithValidationFilter();

shopsGroup.MapGet("shops", async (ShopDbContext dbContext, CancellationToken cancellationToken) =>
{
    return (await dbContext.Shops.ToListAsync(cancellationToken))
     .Select(o => new ShopDto(o.Id, o.Name, o.City, o.Adresas));
});

shopsGroup.MapGet("shops/{shopId}", async (int shopId, ShopDbContext dbContext) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    return Results.Ok(new ShopDto(shop.Id, shop.Name, shop.City, shop.Adresas));
});

shopsGroup.MapPost("shops", async ([Validate] CreateShopDto createShopDto, ShopDbContext dbContext, HttpContext httpContext) =>
{
    var shop = new Shop()
    {
        Name = createShopDto.Name,
        City = createShopDto.City,
        Adresas = createShopDto.Adresas
    };

    dbContext.Shops.Add(shop);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/shops/{shop.Id}",
     new ShopDto(shop.Id, shop.Name, shop.City, shop.Adresas));
});

shopsGroup.MapPut("shops/{shopId}", async (int shopId, [Validate] UpdateShopDto dto, ShopDbContext dbContext) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    shop.Name = dto.Name;
    dbContext.Update(shop);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new ShopDto(shop.Id, shop.Name, shop.City, shop.Adresas));
});

shopsGroup.MapDelete("shops/{shopId}", async (int shopId, ShopDbContext dbContext) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    dbContext.Remove(shop);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

/*
 *  Shops -> Categories
 *  Categories
 */
var categoriesGroup = app.MapGroup("/api/shops/{shopId}").WithValidationFilter();

categoriesGroup.MapGet("categories", async (int shopId, ShopDbContext dbContext, CancellationToken cancellationToken) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var categories = await dbContext.Categories
     .Where(c => c.Shop.Id == shopId) // Filter by shop
     .ToListAsync(cancellationToken);

    return Results.Ok(categories);
});


categoriesGroup.MapGet("categories/{categoryId}", async (int shopId, int categoryId, ShopDbContext dbContext, CancellationToken cancellationToken) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    return Results.Ok(category);
});

categoriesGroup.MapPost("categories", async (int shopId, [Validate] CreateCategoryDto createCategoryDto, ShopDbContext dbContext) =>
{
    // First, check if the shop exists
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = new Category()
    {
        Name = createCategoryDto.Name,
        Shop = shop // Assign the Shop directly
    };

    dbContext.Categories.Add(category);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/shops/{shopId}/categories/{category.Id}",
     new CategoryDto(category.Id, category.Name));
});

categoriesGroup.MapPut("categories/{categoryId}", async (int shopId, int categoryId, [Validate] UpdateCategoryDto dto, ShopDbContext dbContext) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    category.Name = dto.Name;
    dbContext.Update(category);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new CategoryDto(category.Id, category.Name));
});

categoriesGroup.MapDelete("categories/{categoryId}", async (int shopId, int categoryId, ShopDbContext dbContext) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    dbContext.Remove(category);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});


/*
 *  Shops -> Categories -> Products
 *  Products
 */

var productsGroup = app.MapGroup("/api/shops/{shopId}/categories/{categoryId}").WithValidationFilter();

productsGroup.MapGet("products", async (int shopId, int categoryId, ShopDbContext dbContext, CancellationToken cancellationToken) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    var products = await dbContext.Products
     .Where(p => p.Category.Id == categoryId) // Filter by category
     .ToListAsync(cancellationToken);

    return Results.Ok(products);
});



productsGroup.MapGet("products/{productId}", async (int shopId, int categoryId, int productId, ShopDbContext dbContext, CancellationToken cancellationToken) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    var product = await dbContext.Products.FirstOrDefaultAsync(o =>
     o.Id == productId && o.Category.Id == categoryId && o.Category.Shop.Id == shopId);

    return Results.Ok(product);
});

productsGroup.MapPost("products", async (int shopId, int categoryId, [Validate] CreateProductDto createProductDto, ShopDbContext dbContext, HttpContext httpContext) =>
{
    // First, check if the shop exists
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    var product = new Product()
    {
        Name = createProductDto.Name,
        Count = createProductDto.Count,
        Kaina = createProductDto.Kaina,
        CreationDate = DateTime.UtcNow,
        Category = category, // Assign the Category directly
        UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
    };

    dbContext.Products.Add(product);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/shops/{shopId}/categories/{categoryId}/products/{product.Id}",
     new ProductDto(product.Id, product.Name, product.Count, product.Kaina, product.CreationDate));
});

productsGroup.MapPut("products/{productId}", async (int shopId, int categoryId, int productId, [Validate] UpdateProductDto dto, ShopDbContext dbContext, HttpContext httpContext) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    var product = await dbContext.Products.FirstOrDefaultAsync(o =>
     o.Id == productId && o.Category.Id == categoryId && o.Category.Shop.Id == shopId);
    if (product == null)
        return Results.NotFound();


    if (!httpContext.User.IsInRole(ForumRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != product.UserId)
    {
        // NotFound()
        return Results.Forbid();   // .Forbd() Forbidden error.
    }


    product.Name = dto.Name;
    product.Count = dto.Count;
    product.Kaina = dto.Kaina;
    dbContext.Update(product);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new ProductDto(product.Id, product.Name, product.Count, product.Kaina, product.CreationDate));
});

productsGroup.MapDelete("products/{productId}", async (int shopId, int categoryId, int productId, ShopDbContext dbContext) =>
{
    var shop = await dbContext.Shops.FirstOrDefaultAsync(t => t.Id == shopId);
    if (shop == null)
        return Results.NotFound();

    var category = await dbContext.Categories.FirstOrDefaultAsync(p => p.Id == categoryId && p.Shop.Id == shopId);
    if (category == null)
        return Results.NotFound();

    var product = await dbContext.Products.FirstOrDefaultAsync(o =>
     o.Id == productId && o.Category.Id == categoryId && o.Category.Shop.Id == shopId);
    if (product == null)
        return Results.NotFound();

    dbContext.Remove(product);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});


/*
 *  Program Run and additional parameters, functions and etc.
 */
app.AddAuthApi();

app.UseAuthentication();
app.UseAuthorization();

using var scope = app.Services.CreateScope();
var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();

await dbSeeder.SeedAsync();


app.Run();

public record UpdateShopDto(string Name, string City, string Adresas);
public record CreateShopDto(string Name, string City, string Adresas);
public record CreateCategoryDto(string Name);
public record UpdateCategoryDto(string Name);
public record CreateProductDto(string Name, int Count, int Kaina);
public record UpdateProductDto(string Name, int Count, int Kaina);

public class CreateShopDtoValidator : AbstractValidator<CreateShopDto>
{
    public CreateShopDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 100);
        RuleFor(dto => dto.Adresas).NotEmpty().NotNull().Length(min: 4, max: 30);
    }
}

public class UpdateShopDtoValidator : AbstractValidator<UpdateShopDto>
{
    public UpdateShopDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 100);
    }
}

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 100);
    }
}

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 100);
    }
}

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 20);
        // Rule for tokia taisykle kad kai countas 0, tai erroras arba negalimas
    }
}

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 20);
        // Rule for tokia taisykle kad kai countas 0, tai erroras arba negalimas
    }
}