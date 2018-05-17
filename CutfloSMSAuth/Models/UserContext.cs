using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CutfloSMSAuth.Models
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public string SetRegisterSessionId(ref User user)
        {
            user.RegistrationSession = KeyGeneration.GenerateSession();
            return user.RegistrationSession;
        }
    }
}
