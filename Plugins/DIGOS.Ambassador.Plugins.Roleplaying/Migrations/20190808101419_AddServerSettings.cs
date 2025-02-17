﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Migrations
{
    public partial class AddServerSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerSettings",
                schema: "RoleplayModule",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ServerID = table.Column<long>(nullable: true),
                    DedicatedRoleplayChannelsCategory = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ServerSettings_Servers_ServerID",
                        column: x => x.ServerID,
                        principalSchema: "Core",
                        principalTable: "Servers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerSettings_ServerID",
                schema: "RoleplayModule",
                table: "ServerSettings",
                column: "ServerID");

            const string seedQuery =
                "do $$" +
                "    begin" +
                "    if exists(select column_name from information_schema.columns where table_name='Servers' and table_schema='Core' and column_name='DedicatedRoleplayChannelsCategory') then" +
                "        insert into \"RoleplayModule\".\"ServerSettings\"" +
                "        (\"ServerID\", \"DedicatedRoleplayChannelsCategory\")" +
                "        select \"ID\", \"DedicatedRoleplayChannelsCategory\" from \"Core\".\"Servers\";" + "" +
                "    end if;" +
                "    end" +
                "$$;";

            migrationBuilder.Sql(seedQuery);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerSettings",
                schema: "RoleplayModule");
        }
    }
}
