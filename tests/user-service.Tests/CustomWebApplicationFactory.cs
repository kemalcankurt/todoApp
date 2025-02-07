using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using user_service.Data;
using user_service.Models;
using user_service.Services;

namespace user_service.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UserDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                string uniqueDbName = $"TestDb_{Guid.NewGuid()}";

                services.AddDbContext<UserDbContext>(options =>
                {
                    options.UseInMemoryDatabase(uniqueDbName);
                });

                using (var scope = services.BuildServiceProvider().CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<UserDbContext>();
                    db.Database.EnsureCreated();

                    var passwordService = scopedServices.GetRequiredService<IPasswordService>();
                    var hashedPassword = passwordService.HashPassword("Password123");

                    db.Users.Add(new User
                    {
                        Id = 1,
                        Email = "test@todoapp.com",
                        Username = "testUser",
                        PasswordHash = hashedPassword.Hash,
                        PasswordSalt = hashedPassword.Salt,
                        Role = "User"
                    });

                    db.Users.Add(new User
                    {
                        Id = 2,
                        Email = "testAdmin@todoapp.com",
                        Username = "testUserAdmin",
                        PasswordHash = hashedPassword.Hash,
                        PasswordSalt = hashedPassword.Salt,
                        Role = "Admin"
                    });

                    db.SaveChanges();
                }
            });
        }
    }
}
