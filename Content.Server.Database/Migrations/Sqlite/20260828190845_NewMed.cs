using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class NewMed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "body_providers",
                table: "profile",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_body_color_profile_id",
                table: "body_color",
                column: "profile_id");

            migrationBuilder.CreateTable(
                name: "body_color",
                columns: table => new
                {
                    body_color_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    group = table.Column<string>(type: "TEXT", nullable: false),
                    color = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_color", x => x.body_color_id);
                    table.ForeignKey(
                        name: "FK_body_color_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.DropColumn(
                name: "clown_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "custom_specie_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "cyborg_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "display_pronouns",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "eye_color",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "facial_hair_color",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "facial_hair_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_color",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "mime_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "station_ai_name",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "skin_color",
                table: "profile");

            migrationBuilder.RenameColumn(
                name: "bark_voice",
                table: "profile",
                newName: "bark");

            migrationBuilder.RenameColumn(
                name: "flavor_text",
                table: "profile",
                newName: "flavor");

            migrationBuilder.Sql("UPDATE profile SET markings = '[]'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "clown_name",
                table: "profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_specie_name",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cyborg_name",
                table: "profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_pronouns",
                table: "profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eye_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "facial_hair_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "facial_hair_name",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hair_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hair_name",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "mime_name",
                table: "profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "skin_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "station_ai_name",
                table: "profile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "body_providers",
                table: "profile");

            migrationBuilder.DropTable(
                name: "body_color");

            migrationBuilder.RenameColumn(
                name: "bark",
                table: "profile",
                newName: "bark_voice");

            migrationBuilder.RenameColumn(
                name: "flavor",
                table: "profile",
                newName: "flavor_text");

            migrationBuilder.Sql("UPDATE profile SET markings = '[]'");
        }
    }
}
