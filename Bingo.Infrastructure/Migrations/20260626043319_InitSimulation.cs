using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bingo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "draws",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DrawAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WinningResult = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    D1 = table.Column<int>(type: "integer", nullable: false),
                    D2 = table.Column<int>(type: "integer", nullable: false),
                    D3 = table.Column<int>(type: "integer", nullable: false),
                    Sum = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    IsTriple = table.Column<bool>(type: "boolean", nullable: false),
                    TripleDigit = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draws", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prize_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BetKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BetValue = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prize_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    IsAdaptive = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultParamsJson = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategies", x => x.Id);
                    table.UniqueConstraint("AK_strategies_Key", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "sim_users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StrategyKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigJson = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sim_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sim_users_strategies_StrategyKey",
                        column: x => x.StrategyKey,
                        principalTable: "strategies",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_accounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimUserId = table.Column<int>(type: "integer", nullable: false),
                    GameDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartingBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TicketsBought = table.Column<int>(type: "integer", nullable: false),
                    TotalStaked = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    IsBusted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_accounts_sim_users_SimUserId",
                        column: x => x.SimUserId,
                        principalTable: "sim_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_stats",
                columns: table => new
                {
                    SimUserId = table.Column<int>(type: "integer", nullable: false),
                    TotalTickets = table.Column<int>(type: "integer", nullable: false),
                    TotalWins = table.Column<int>(type: "integer", nullable: false),
                    WinRate = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    TotalStaked = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Roi = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DaysPlayed = table.Column<int>(type: "integer", nullable: false),
                    DaysBusted = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_stats", x => x.SimUserId);
                    table.ForeignKey(
                        name: "FK_user_stats_sim_users_SimUserId",
                        column: x => x.SimUserId,
                        principalTable: "sim_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_strategy_states",
                columns: table => new
                {
                    SimUserId = table.Column<int>(type: "integer", nullable: false),
                    StateJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_strategy_states", x => x.SimUserId);
                    table.ForeignKey(
                        name: "FK_user_strategy_states_sim_users_SimUserId",
                        column: x => x.SimUserId,
                        principalTable: "sim_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimUserId = table.Column<int>(type: "integer", nullable: false),
                    DailyAccountId = table.Column<long>(type: "bigint", nullable: false),
                    TargetDrawAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DrawId = table.Column<long>(type: "bigint", nullable: true),
                    BetKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BetValue = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Stake = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    IsSettled = table.Column<bool>(type: "boolean", nullable: false),
                    IsWin = table.Column<bool>(type: "boolean", nullable: false),
                    Payout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Profit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlacedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tickets_daily_accounts_DailyAccountId",
                        column: x => x.DailyAccountId,
                        principalTable: "daily_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tickets_draws_DrawId",
                        column: x => x.DrawId,
                        principalTable: "draws",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tickets_sim_users_SimUserId",
                        column: x => x.SimUserId,
                        principalTable: "sim_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_accounts_SimUserId_GameDate",
                table: "daily_accounts",
                columns: new[] { "SimUserId", "GameDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draws_DrawAt",
                table: "draws",
                column: "DrawAt",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prize_rules_BetKind_BetValue",
                table: "prize_rules",
                columns: new[] { "BetKind", "BetValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sim_users_StrategyKey",
                table: "sim_users",
                column: "StrategyKey");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_Key",
                table: "strategies",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_DailyAccountId",
                table: "tickets",
                column: "DailyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_DrawId",
                table: "tickets",
                column: "DrawId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_IsSettled",
                table: "tickets",
                column: "IsSettled");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_SimUserId_TargetDrawAt",
                table: "tickets",
                columns: new[] { "SimUserId", "TargetDrawAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prize_rules");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "user_stats");

            migrationBuilder.DropTable(
                name: "user_strategy_states");

            migrationBuilder.DropTable(
                name: "daily_accounts");

            migrationBuilder.DropTable(
                name: "draws");

            migrationBuilder.DropTable(
                name: "sim_users");

            migrationBuilder.DropTable(
                name: "strategies");
        }
    }
}
