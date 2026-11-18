using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewDrill.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261116133001_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                BrowserToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "JobDescriptions",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                RawTextHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ParsedProfileJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobDescriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_JobDescriptions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "InterviewSessions",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                JobDescriptionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                InterviewerStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InterviewSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_InterviewSessions_JobDescriptions_JobDescriptionId",
                    column: x => x.JobDescriptionId,
                    principalTable: "JobDescriptions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_InterviewSessions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Questions",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                SessionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                JobDescriptionHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                NormalizedQuestionHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SkillTarget = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Embedding = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                AskedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Questions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Questions_InterviewSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "InterviewSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "StudyGuides",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                SessionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Markdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StudyGuides", x => x.Id);
                table.ForeignKey(
                    name: "FK_StudyGuides_InterviewSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "InterviewSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Answers",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                QuestionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EvaluationPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AnsweredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Answers", x => x.Id);
                table.ForeignKey(
                    name: "FK_Answers_Questions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "Questions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Answers_QuestionId",
            table: "Answers",
            column: "QuestionId");

        migrationBuilder.CreateIndex(
            name: "IX_InterviewSessions_JobDescriptionId",
            table: "InterviewSessions",
            column: "JobDescriptionId");

        migrationBuilder.CreateIndex(
            name: "IX_InterviewSessions_UserId",
            table: "InterviewSessions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_JobDescriptions_UserId_RawTextHash",
            table: "JobDescriptions",
            columns: new[] { "UserId", "RawTextHash" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Questions_SessionId_AskedAt",
            table: "Questions",
            columns: new[] { "SessionId", "AskedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Questions_UserId_JobDescriptionHash_NormalizedQuestionHash",
            table: "Questions",
            columns: new[] { "UserId", "JobDescriptionHash", "NormalizedQuestionHash" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StudyGuides_SessionId",
            table: "StudyGuides",
            column: "SessionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_BrowserToken",
            table: "Users",
            column: "BrowserToken",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Answers");

        migrationBuilder.DropTable(
            name: "StudyGuides");

        migrationBuilder.DropTable(
            name: "Questions");

        migrationBuilder.DropTable(
            name: "InterviewSessions");

        migrationBuilder.DropTable(
            name: "JobDescriptions");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
