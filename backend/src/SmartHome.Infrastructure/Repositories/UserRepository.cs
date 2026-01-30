using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Entities;
using SmartHome.Domain.Interfaces.User;
using SmartHome.Infrastructure.Persistence;

namespace SmartHome.Infrastructure.Repositories;

public class UserRepository(SmartHomeDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task AddAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }


    public async Task<bool> IsEmailTakenAsync(string email)
    {
        return await context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }
}