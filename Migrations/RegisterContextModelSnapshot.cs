﻿// <auto-generated />
using System;
using BaristaHome.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BaristaHome.Migrations
{
    [DbContext(typeof(BaristaHomeContext))]
    partial class RegisterContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("BaristaHome.Models.Role", b =>
                {
                    b.Property<int>("RoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("RoleId"), 1L, 1);

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.HasKey("RoleId");

                    b.ToTable("Role");
                });

            modelBuilder.Entity("BaristaHome.Models.ShiftStatus", b =>
                {
                    b.Property<int>("ShiftStatusId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ShiftStatusId"), 1L, 1);

                    b.Property<string>("ShiftStatusName")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.HasKey("ShiftStatusId");

                    b.ToTable("ShiftStatus");
                });

            modelBuilder.Entity("BaristaHome.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"), 1L, 1);

                    b.Property<string>("Color")
                        .HasMaxLength(7)
                        .HasColumnType("nvarchar(7)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("InviteCode")
                        .HasMaxLength(5)
                        .HasColumnType("nvarchar(5)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("UserId");

                    b.HasIndex("RoleId");

                    b.ToTable("User");
                });

            modelBuilder.Entity("BaristaHome.Models.UserShiftStatus", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("ShiftStatusId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Time")
                        .HasColumnType("datetime2");

                    b.HasKey("UserId", "ShiftStatusId", "Time");

                    b.HasIndex("ShiftStatusId");

                    b.ToTable("UserShiftStatus");
                });

            modelBuilder.Entity("BaristaHome.Models.User", b =>
                {
                    b.HasOne("BaristaHome.Models.Role", "Role")
                        .WithMany("Users")
                        .HasForeignKey("RoleId");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("BaristaHome.Models.UserShiftStatus", b =>
                {
                    b.HasOne("BaristaHome.Models.ShiftStatus", "ShiftStatus")
                        .WithMany("UserShiftStatuses")
                        .HasForeignKey("ShiftStatusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BaristaHome.Models.User", "User")
                        .WithMany("UserShiftStatuses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ShiftStatus");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BaristaHome.Models.Role", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("BaristaHome.Models.ShiftStatus", b =>
                {
                    b.Navigation("UserShiftStatuses");
                });

            modelBuilder.Entity("BaristaHome.Models.User", b =>
                {
                    b.Navigation("UserShiftStatuses");
                });
#pragma warning restore 612, 618
        }
    }
}
