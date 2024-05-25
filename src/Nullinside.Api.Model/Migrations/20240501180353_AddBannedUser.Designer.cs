﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nullinside.Api.Model;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    [DbContext(typeof(NullinsideContext))]
    [Migration("20240501180353_AddBannedUser")]
    partial class AddBannedUser
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.DockerDeployments", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<bool>("IsDockerComposeProject")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<string>("Notes")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ServerDir")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("DisplayName");

                    b.HasIndex("IsDockerComposeProject", "Name");

                    b.ToTable("DockerDeployments");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.TwitchBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("BannedUserId")
                        .HasColumnType("int");

                    b.Property<int>("ChannelId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BannedUserId");

                    b.HasIndex("ChannelId");

                    b.ToTable("TwitchBan");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.TwitchUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("TwitchId")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("TwitchUsername")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("TwitchId")
                        .IsUnique();

                    b.ToTable("TwitchUser");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.TwitchUserConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("BanKnownBots")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Enabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("TwitchUserConfig");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Email")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("IsBanned")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Token")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<int?>("TwitchConfigId")
                        .HasColumnType("int");

                    b.Property<string>("TwitchId")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("TwitchLastScanned")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("TwitchRefreshToken")
                        .HasColumnType("longtext");

                    b.Property<string>("TwitchToken")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("TwitchTokenExpiration")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("TwitchUsername")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("TwitchConfigId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.UserRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.Property<DateTime>("RoleAdded")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.TwitchBan", b =>
                {
                    b.HasOne("Nullinside.Api.Model.Ddl.TwitchUser", "BannedUser")
                        .WithMany()
                        .HasForeignKey("BannedUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Nullinside.Api.Model.Ddl.User", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BannedUser");

                    b.Navigation("Channel");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.User", b =>
                {
                    b.HasOne("Nullinside.Api.Model.Ddl.TwitchUserConfig", "TwitchConfig")
                        .WithMany()
                        .HasForeignKey("TwitchConfigId");

                    b.Navigation("TwitchConfig");
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.UserRole", b =>
                {
                    b.HasOne("Nullinside.Api.Model.Ddl.User", null)
                        .WithMany("Roles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Nullinside.Api.Model.Ddl.User", b =>
                {
                    b.Navigation("Roles");
                });
#pragma warning restore 612, 618
        }
    }
}
