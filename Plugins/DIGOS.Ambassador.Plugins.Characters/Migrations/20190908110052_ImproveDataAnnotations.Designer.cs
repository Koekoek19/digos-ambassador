﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using System;
using DIGOS.Ambassador.Plugins.Characters.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DIGOS.Ambassador.Plugins.Characters.Migrations
{
    [DbContext(typeof(CharactersDatabaseContext))]
    [Migration("20190908110052_ImproveDataAnnotations")]
    partial class ImproveDataAnnotations
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("CharacterModule")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AvatarUrl")
                        .IsRequired();

                    b.Property<string>("Description")
                        .IsRequired();

                    b.Property<bool>("IsCurrent");

                    b.Property<bool>("IsDefault");

                    b.Property<bool>("IsNSFW");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<string>("Nickname");

                    b.Property<long>("OwnerID");

                    b.Property<string>("PronounProviderFamily")
                        .IsRequired();

                    b.Property<long?>("RoleID");

                    b.Property<long>("ServerID");

                    b.Property<string>("Summary")
                        .IsRequired();

                    b.HasKey("ID");

                    b.HasIndex("OwnerID");

                    b.HasIndex("RoleID");

                    b.ToTable("Characters","CharacterModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Access");

                    b.Property<long>("DiscordID");

                    b.Property<long>("ServerID");

                    b.HasKey("ID");

                    b.HasIndex("ServerID");

                    b.ToTable("CharacterRoles","CharacterModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Data.Image", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Caption")
                        .IsRequired();

                    b.Property<long?>("CharacterID");

                    b.Property<bool>("IsNSFW");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<string>("Url")
                        .IsRequired();

                    b.HasKey("ID");

                    b.HasIndex("CharacterID");

                    b.ToTable("Images","CharacterModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<long>("DiscordID");

                    b.Property<bool>("IsNSFW");

                    b.Property<string>("JoinMessage");

                    b.Property<bool>("SendJoinMessage");

                    b.Property<bool>("SuppressPermissonWarnings");

                    b.HasKey("ID");

                    b.ToTable("Servers","Core");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.ServerUser", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ServerID");

                    b.Property<long>("UserID");

                    b.HasKey("ID");

                    b.HasIndex("ServerID");

                    b.HasIndex("UserID");

                    b.ToTable("ServerUser","Core");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.User", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Bio");

                    b.Property<long>("DiscordID");

                    b.Property<int?>("Timezone");

                    b.HasKey("ID");

                    b.ToTable("Users","Core");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Data.Image", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Characters.Model.Character")
                        .WithMany("Images")
                        .HasForeignKey("CharacterID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.ServerUser", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany("KnownUsers")
                        .HasForeignKey("ServerID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
