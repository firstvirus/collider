using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Domain.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Первичный ключ типа события")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Наименование типа события")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Первичный ключ пользователя"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Полное имя пользователя"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Дата и время регистрации пользователя")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, comment: "Первичный ключ события")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Внешний ключ на таблицу users"),
                    type_id = table.Column<int>(type: "integer", nullable: false, comment: "Внешний ключ на таблицу event_types"),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Дата и время возникновения события"),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, comment: "Дополнительные данные события в формате JSON"),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    type_id1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_events_event_types_type_id1",
                        column: x => x.type_id1,
                        principalTable: "event_types",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_events_users_user_id1",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_events_timestamp",
                table: "events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_events_type_id",
                table: "events",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_type_id1",
                table: "events",
                column: "type_id1");

            migrationBuilder.CreateIndex(
                name: "ix_events_user_id",
                table: "events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_user_id1",
                table: "events",
                column: "user_id1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "event_types");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
