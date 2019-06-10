﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using nhitomi.Core;

namespace nhitomi.Core.Migrations
{
    [DbContext(typeof(nhitomiDbContext))]
    [Migration("20190609074127_RemoveDoujinDenormalized")]
    partial class RemoveDoujinDenormalized
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("nhitomi.Core.Collection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(32);

                    b.Property<ulong>("OwnerId");

                    b.Property<int>("Sort");

                    b.Property<bool>("SortDescending");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.ToTable("Collections");
                });

            modelBuilder.Entity("nhitomi.Core.CollectionRef", b =>
                {
                    b.Property<int>("CollectionId");

                    b.Property<int>("DoujinId");

                    b.HasKey("CollectionId", "DoujinId");

                    b.HasIndex("DoujinId");

                    b.ToTable("CollectionRef");
                });

            modelBuilder.Entity("nhitomi.Core.Doujin", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("AccessId");

                    b.Property<string>("Data")
                        .HasMaxLength(4096);

                    b.Property<string>("OriginalName")
                        .IsRequired()
                        .HasMaxLength(256);

                    b.Property<int>("PageCount");

                    b.Property<string>("PrettyName")
                        .IsRequired()
                        .HasMaxLength(256);

                    b.Property<DateTime>("ProcessTime");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(16);

                    b.Property<string>("SourceId")
                        .IsRequired()
                        .HasMaxLength(16);

                    b.Property<DateTime>("UploadTime");

                    b.HasKey("Id");

                    b.HasIndex("AccessId")
                        .IsUnique();

                    b.HasIndex("ProcessTime");

                    b.HasIndex("UploadTime");

                    b.HasIndex("Source", "SourceId");

                    b.ToTable("Doujins");
                });

            modelBuilder.Entity("nhitomi.Core.FeedChannel", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("GuildId");

                    b.Property<int>("LastDoujinId");

                    b.Property<int>("WhitelistType");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("LastDoujinId");

                    b.ToTable("FeedChannels");
                });

            modelBuilder.Entity("nhitomi.Core.FeedChannelTag", b =>
                {
                    b.Property<ulong>("FeedChannelId");

                    b.Property<int>("TagId");

                    b.HasKey("FeedChannelId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("FeedChannelTag");
                });

            modelBuilder.Entity("nhitomi.Core.Guild", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Language");

                    b.Property<bool?>("SearchQualityFilter");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("nhitomi.Core.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("AccessId");

                    b.Property<int>("Type");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasMaxLength(128);

                    b.HasKey("Id");

                    b.HasIndex("AccessId")
                        .IsUnique();

                    b.HasIndex("Value");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("nhitomi.Core.TagRef", b =>
                {
                    b.Property<int>("DoujinId");

                    b.Property<int>("TagId");

                    b.HasKey("DoujinId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("TagRef");
                });

            modelBuilder.Entity("nhitomi.Core.CollectionRef", b =>
                {
                    b.HasOne("nhitomi.Core.Collection", "Collection")
                        .WithMany("Doujins")
                        .HasForeignKey("CollectionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("nhitomi.Core.Doujin", "Doujin")
                        .WithMany("Collections")
                        .HasForeignKey("DoujinId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("nhitomi.Core.FeedChannel", b =>
                {
                    b.HasOne("nhitomi.Core.Guild", "Guild")
                        .WithMany("FeedChannels")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("nhitomi.Core.Doujin", "LastDoujin")
                        .WithMany("FeedChannels")
                        .HasForeignKey("LastDoujinId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("nhitomi.Core.FeedChannelTag", b =>
                {
                    b.HasOne("nhitomi.Core.FeedChannel", "FeedChannel")
                        .WithMany("Tags")
                        .HasForeignKey("FeedChannelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("nhitomi.Core.Tag", "Tag")
                        .WithMany("FeedChannels")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("nhitomi.Core.TagRef", b =>
                {
                    b.HasOne("nhitomi.Core.Doujin", "Doujin")
                        .WithMany("Tags")
                        .HasForeignKey("DoujinId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("nhitomi.Core.Tag", "Tag")
                        .WithMany("Doujins")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}