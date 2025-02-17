﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using System;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DIGOS.Ambassador.Plugins.Transformations.Migrations
{
    [DbContext(typeof(TransformationsDatabaseContext))]
    [Migration("20190831191030_CascadeComponentDeletions")]
    partial class CascadeComponentDeletions
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("TransformationModule")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AvatarUrl");

                    b.Property<string>("Description");

                    b.Property<bool>("IsCurrent");

                    b.Property<bool>("IsDefault");

                    b.Property<bool>("IsNSFW");

                    b.Property<string>("Name");

                    b.Property<string>("Nickname");

                    b.Property<long>("OwnerID");

                    b.Property<string>("PronounProviderFamily");

                    b.Property<long?>("RoleID");

                    b.Property<long>("ServerID");

                    b.Property<string>("Summary");

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

                    b.Property<string>("Caption");

                    b.Property<long?>("CharacterID");

                    b.Property<bool>("IsNSFW");

                    b.Property<string>("Name");

                    b.Property<string>("Url");

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

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Appearance", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("GenderScale");

                    b.Property<double>("Height");

                    b.Property<double>("Muscularity");

                    b.Property<double>("Weight");

                    b.HasKey("ID");

                    b.ToTable("Appearances","TransformationModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.AppearanceComponent", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<long?>("AppearanceID");

                    b.Property<int>("Chirality");

                    b.Property<int?>("Pattern");

                    b.Property<int>("Size");

                    b.Property<long>("TransformationID");

                    b.HasKey("ID");

                    b.HasIndex("AppearanceID");

                    b.HasIndex("TransformationID");

                    b.ToTable("AppearanceComponents","TransformationModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.AppearanceConfiguration", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("CharacterID");

                    b.Property<long>("CurrentAppearanceID");

                    b.Property<long>("DefaultAppearanceID");

                    b.HasKey("ID");

                    b.HasIndex("CharacterID");

                    b.HasIndex("CurrentAppearanceID");

                    b.HasIndex("DefaultAppearanceID");

                    b.ToTable("AppearanceConfigurations","TransformationModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.GlobalUserProtection", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("DefaultOptIn");

                    b.Property<int>("DefaultType");

                    b.Property<long>("UserID");

                    b.HasKey("ID");

                    b.HasIndex("UserID");

                    b.ToTable("GlobalUserProtections","TransformationModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.ServerUserProtection", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("HasOptedIn");

                    b.Property<long>("ServerID");

                    b.Property<int>("Type");

                    b.Property<long>("UserID");

                    b.HasKey("ID");

                    b.HasIndex("ServerID");

                    b.HasIndex("UserID");

                    b.ToTable("ServerUserProtections","TransformationModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Species", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description")
                        .IsRequired();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<long?>("ParentID");

                    b.HasKey("ID");

                    b.HasIndex("ParentID");

                    b.ToTable("Species","TransformationModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("DefaultPattern");

                    b.Property<string>("Description")
                        .IsRequired();

                    b.Property<string>("GrowMessage")
                        .IsRequired();

                    b.Property<bool>("IsNSFW");

                    b.Property<int>("Part");

                    b.Property<string>("ShiftMessage")
                        .IsRequired();

                    b.Property<string>("SingleDescription")
                        .IsRequired();

                    b.Property<long>("SpeciesID");

                    b.Property<string>("UniformDescription");

                    b.Property<string>("UniformGrowMessage");

                    b.Property<string>("UniformShiftMessage");

                    b.HasKey("ID");

                    b.HasIndex("SpeciesID");

                    b.ToTable("Transformations","TransformationModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.UserProtectionEntry", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("GlobalProtectionID");

                    b.Property<int>("Type");

                    b.Property<long>("UserID");

                    b.HasKey("ID");

                    b.HasIndex("GlobalProtectionID");

                    b.HasIndex("UserID");

                    b.ToTable("UserProtectionEntries","TransformationModule");
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

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.AppearanceComponent", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Appearance")
                        .WithMany("Components")
                        .HasForeignKey("AppearanceID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation", "Transformation")
                        .WithMany()
                        .HasForeignKey("TransformationID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "BaseColour", b1 =>
                        {
                            b1.Property<long>("AppearanceComponentID");

                            b1.Property<int?>("Modifier");

                            b1.Property<int>("Shade");

                            b1.HasKey("AppearanceComponentID");

                            b1.ToTable("BaseColours","TransformationModule");

                            b1.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.AppearanceComponent")
                                .WithOne("BaseColour")
                                .HasForeignKey("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "AppearanceComponentID")
                                .OnDelete(DeleteBehavior.Cascade);
                        });

                    b.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "PatternColour", b1 =>
                        {
                            b1.Property<long>("AppearanceComponentID");

                            b1.Property<int?>("Modifier");

                            b1.Property<int>("Shade");

                            b1.HasKey("AppearanceComponentID");

                            b1.ToTable("PatternColours","TransformationModule");

                            b1.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.AppearanceComponent")
                                .WithOne("PatternColour")
                                .HasForeignKey("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "AppearanceComponentID")
                                .OnDelete(DeleteBehavior.Cascade);
                        });
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.AppearanceConfiguration", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Characters.Model.Character", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Appearance", "CurrentAppearance")
                        .WithMany()
                        .HasForeignKey("CurrentAppearanceID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Appearance", "DefaultAppearance")
                        .WithMany()
                        .HasForeignKey("DefaultAppearanceID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.GlobalUserProtection", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.ServerUserProtection", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Species", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Species", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentID");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Species", "Species")
                        .WithMany()
                        .HasForeignKey("SpeciesID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "DefaultBaseColour", b1 =>
                        {
                            b1.Property<long>("TransformationID");

                            b1.Property<int?>("Modifier");

                            b1.Property<int>("Shade");

                            b1.HasKey("TransformationID");

                            b1.ToTable("DefaultBaseColours","TransformationModule");

                            b1.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation")
                                .WithOne("DefaultBaseColour")
                                .HasForeignKey("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "TransformationID")
                                .OnDelete(DeleteBehavior.Cascade);
                        });

                    b.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "DefaultPatternColour", b1 =>
                        {
                            b1.Property<long>("TransformationID");

                            b1.Property<int?>("Modifier");

                            b1.Property<int>("Shade");

                            b1.HasKey("TransformationID");

                            b1.ToTable("DefaultPatternColours","TransformationModule");

                            b1.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation")
                                .WithOne("DefaultPatternColour")
                                .HasForeignKey("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "TransformationID")
                                .OnDelete(DeleteBehavior.Cascade);
                        });
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.UserProtectionEntry", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.GlobalUserProtection", "GlobalProtection")
                        .WithMany("UserListing")
                        .HasForeignKey("GlobalProtectionID")
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
