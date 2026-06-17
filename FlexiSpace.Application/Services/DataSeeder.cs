using FlexiSpace.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FlexiSpace.Web.Extensions
{
    public static class DataSeeder
    {
        public static async Task SeedAdminAccountAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var seederService = scope.ServiceProvider.GetRequiredService<IDataSeederService>();
            await seederService.SeedAdminAccountAsync();
        }
    }
}