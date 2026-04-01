using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meran.Back.Migrations
{
    /// <inheritdoc />
    public partial class SaasArchitectureRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_events_application_users_application_user_id",
                table: "payment_events");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "payment_events",
                newName: "event_type");

            migrationBuilder.RenameColumn(
                name: "application_user_id",
                table: "payment_events",
                newName: "subscription_id");

            migrationBuilder.RenameIndex(
                name: "IX_payment_events_application_user_id",
                table: "payment_events",
                newName: "IX_payment_events_subscription_id");

            migrationBuilder.RenameIndex(
                name: "IX_payment_events_application_id_application_user_id_occurred_~",
                table: "payment_events",
                newName: "IX_payment_events_application_id_subscription_id_occurred_at");

            migrationBuilder.RenameColumn(
                name: "plan",
                table: "application_users",
                newName: "Plan");

            migrationBuilder.RenameColumn(
                name: "payment_reference",
                table: "application_users",
                newName: "PaymentReference");

            migrationBuilder.RenameColumn(
                name: "payment_provider",
                table: "application_users",
                newName: "PaymentProvider");

            migrationBuilder.RenameColumn(
                name: "next_payment_due_at",
                table: "application_users",
                newName: "NextPaymentDueAt");

            migrationBuilder.RenameColumn(
                name: "last_payment_currency",
                table: "application_users",
                newName: "LastPaymentCurrency");

            migrationBuilder.RenameColumn(
                name: "last_payment_at",
                table: "application_users",
                newName: "LastPaymentAt");

            migrationBuilder.RenameColumn(
                name: "last_payment_amount",
                table: "application_users",
                newName: "LastPaymentAmount");

            migrationBuilder.RenameColumn(
                name: "has_payment_issue",
                table: "application_users",
                newName: "HasPaymentIssue");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "payment_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Plan",
                table: "application_users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentReference",
                table: "application_users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentProvider",
                table: "application_users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastPaymentCurrency",
                table: "application_users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LastPaymentAmount",
                table: "application_users",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "application_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_features", x => x.id);
                    table.ForeignKey(
                        name: "FK_application_features_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "application_user_roles",
                columns: table => new
                {
                    application_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_user_roles", x => new { x.application_user_id, x.role });
                    table.ForeignKey(
                        name: "FK_application_user_roles_application_users_application_user_id",
                        column: x => x.application_user_id,
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trial_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscriptions_application_plans_application_plan_id",
                        column: x => x.application_plan_id,
                        principalTable: "application_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_application_users_application_user_id",
                        column: x => x.application_user_id,
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "application_plan_feature_values",
                columns: table => new
                {
                    application_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_plan_feature_values", x => new { x.application_plan_id, x.application_feature_id });
                    table.ForeignKey(
                        name: "FK_application_plan_feature_values_application_features_applic~",
                        column: x => x.application_feature_id,
                        principalTable: "application_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_application_plan_feature_values_application_plans_applicati~",
                        column: x => x.application_plan_id,
                        principalTable: "application_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_features_application_id_key",
                table: "application_features",
                columns: new[] { "application_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_plan_feature_values_application_feature_id",
                table: "application_plan_feature_values",
                column: "application_feature_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_application_plan_id",
                table: "subscriptions",
                column: "application_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_application_user_id_status",
                table: "subscriptions",
                columns: new[] { "application_user_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "FK_payment_events_subscriptions_subscription_id",
                table: "payment_events",
                column: "subscription_id",
                principalTable: "subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_events_subscriptions_subscription_id",
                table: "payment_events");

            migrationBuilder.DropTable(
                name: "application_plan_feature_values");

            migrationBuilder.DropTable(
                name: "application_user_roles");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "application_features");

            migrationBuilder.DropColumn(
                name: "status",
                table: "payment_events");

            migrationBuilder.RenameColumn(
                name: "subscription_id",
                table: "payment_events",
                newName: "application_user_id");

            migrationBuilder.RenameColumn(
                name: "event_type",
                table: "payment_events",
                newName: "type");

            migrationBuilder.RenameIndex(
                name: "IX_payment_events_subscription_id",
                table: "payment_events",
                newName: "IX_payment_events_application_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_payment_events_application_id_subscription_id_occurred_at",
                table: "payment_events",
                newName: "IX_payment_events_application_id_application_user_id_occurred_~");

            migrationBuilder.RenameColumn(
                name: "Plan",
                table: "application_users",
                newName: "plan");

            migrationBuilder.RenameColumn(
                name: "PaymentReference",
                table: "application_users",
                newName: "payment_reference");

            migrationBuilder.RenameColumn(
                name: "PaymentProvider",
                table: "application_users",
                newName: "payment_provider");

            migrationBuilder.RenameColumn(
                name: "NextPaymentDueAt",
                table: "application_users",
                newName: "next_payment_due_at");

            migrationBuilder.RenameColumn(
                name: "LastPaymentCurrency",
                table: "application_users",
                newName: "last_payment_currency");

            migrationBuilder.RenameColumn(
                name: "LastPaymentAt",
                table: "application_users",
                newName: "last_payment_at");

            migrationBuilder.RenameColumn(
                name: "LastPaymentAmount",
                table: "application_users",
                newName: "last_payment_amount");

            migrationBuilder.RenameColumn(
                name: "HasPaymentIssue",
                table: "application_users",
                newName: "has_payment_issue");

            migrationBuilder.AlterColumn<string>(
                name: "plan",
                table: "application_users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payment_reference",
                table: "application_users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payment_provider",
                table: "application_users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_payment_currency",
                table: "application_users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "last_payment_amount",
                table: "application_users",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_events_application_users_application_user_id",
                table: "payment_events",
                column: "application_user_id",
                principalTable: "application_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
