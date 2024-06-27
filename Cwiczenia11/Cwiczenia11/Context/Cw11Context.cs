using System;
using System.Collections.Generic;
using Cwiczenia11.Models;
using Microsoft.EntityFrameworkCore;

namespace Cwiczenia11.Context;

public partial class Cw11Context : DbContext
{
    public Cw11Context()
    {
    }

    public Cw11Context(DbContextOptions<Cw11Context> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
