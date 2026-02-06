using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MedView.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialMySqlMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HangingProtocols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Modality = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyPart = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LayoutConfig = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangingProtocols", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Studies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudyInstanceUid = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StudyId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StudyDescription = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StudyDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StudyTime = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessionNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferringPhysicianName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PatientId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PatientName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PatientBirthDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PatientSex = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PatientAge = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstitutionName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NumberOfSeries = table.Column<int>(type: "int", nullable: false),
                    NumberOfInstances = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studies", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultLayout = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultWindowPresets = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowAnnotations = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowMeasurements = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowOverlay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DefaultTool = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MeasurementColor = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnnotationColor = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SeriesInstanceUid = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeriesNumber = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeriesDescription = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Modality = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeriesDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SeriesTime = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyPartExamined = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProtocolName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rows = table.Column<int>(type: "int", nullable: true),
                    Columns = table.Column<int>(type: "int", nullable: true),
                    SliceThickness = table.Column<double>(type: "double", nullable: true),
                    SpacingBetweenSlices = table.Column<double>(type: "double", nullable: true),
                    ImageOrientationPatient = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumberOfInstances = table.Column<int>(type: "int", nullable: false),
                    StudyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Series_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Instances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SopInstanceUid = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SopClassUid = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstanceNumber = table.Column<int>(type: "int", nullable: true),
                    FilePath = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    Rows = table.Column<int>(type: "int", nullable: true),
                    Columns = table.Column<int>(type: "int", nullable: true),
                    BitsAllocated = table.Column<int>(type: "int", nullable: true),
                    BitsStored = table.Column<int>(type: "int", nullable: true),
                    HighBit = table.Column<int>(type: "int", nullable: true),
                    PhotometricInterpretation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SamplesPerPixel = table.Column<int>(type: "int", nullable: true),
                    PixelRepresentation = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WindowCenter = table.Column<double>(type: "double", nullable: true),
                    WindowWidth = table.Column<double>(type: "double", nullable: true),
                    RescaleIntercept = table.Column<double>(type: "double", nullable: true),
                    RescaleSlope = table.Column<double>(type: "double", nullable: true),
                    ImagePositionPatient = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageOrientationPatient = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PixelSpacing = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SliceLocation = table.Column<double>(type: "double", nullable: true),
                    NumberOfFrames = table.Column<int>(type: "int", nullable: false),
                    FrameTime = table.Column<double>(type: "double", nullable: true),
                    TransferSyntaxUid = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeriesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Instances_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Annotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FontSize = table.Column<double>(type: "double", nullable: true),
                    IsVisible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PositionData = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FrameNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstanceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Annotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Annotations_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<double>(type: "double", nullable: true),
                    Unit = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsVisible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mean = table.Column<double>(type: "double", nullable: true),
                    StdDev = table.Column<double>(type: "double", nullable: true),
                    Min = table.Column<double>(type: "double", nullable: true),
                    Max = table.Column<double>(type: "double", nullable: true),
                    Area = table.Column<double>(type: "double", nullable: true),
                    PositionData = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FrameNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstanceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurements_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "HangingProtocols",
                columns: new[] { "Id", "BodyPart", "CreatedAt", "CreatedBy", "Description", "IsActive", "IsDefault", "LayoutConfig", "Modality", "Name", "Priority" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 1, 17, 18, 48, 39, 46, DateTimeKind.Utc).AddTicks(805), null, "Default layout for CT studies", true, true, "{\n                    \"rows\": 1,\n                    \"columns\": 1,\n                    \"viewports\": [\n                        {\"position\": 0, \"seriesIndex\": 0}\n                    ]\n                }", "CT", "CT Default", 100 },
                    { 2, "HEAD", new DateTime(2026, 1, 17, 18, 48, 39, 46, DateTimeKind.Utc).AddTicks(808), null, "Multi-sequence brain MRI layout", true, false, "{\n                    \"rows\": 2,\n                    \"columns\": 2,\n                    \"viewports\": [\n                        {\"position\": 0, \"seriesDescription\": \"T1\"},\n                        {\"position\": 1, \"seriesDescription\": \"T2\"},\n                        {\"position\": 2, \"seriesDescription\": \"FLAIR\"},\n                        {\"position\": 3, \"seriesDescription\": \"DWI\"}\n                    ]\n                }", "MR", "MR Brain", 90 },
                    { 3, "CHEST", new DateTime(2026, 1, 17, 18, 48, 39, 46, DateTimeKind.Utc).AddTicks(811), null, "Side-by-side comparison for chest X-rays", true, false, "{\n                    \"rows\": 1,\n                    \"columns\": 2,\n                    \"viewports\": [\n                        {\"position\": 0, \"studyIndex\": 0, \"seriesIndex\": 0},\n                        {\"position\": 1, \"studyIndex\": 1, \"seriesIndex\": 0}\n                    ],\n                    \"enablePriorComparison\": true\n                }", "CR", "Chest X-Ray Comparison", 80 },
                    { 4, null, new DateTime(2026, 1, 17, 18, 48, 39, 46, DateTimeKind.Utc).AddTicks(813), null, "Standard mammography display", true, false, "{\n                    \"rows\": 2,\n                    \"columns\": 2,\n                    \"viewports\": [\n                        {\"position\": 0, \"viewPosition\": \"MLO\", \"laterality\": \"R\"},\n                        {\"position\": 1, \"viewPosition\": \"MLO\", \"laterality\": \"L\"},\n                        {\"position\": 2, \"viewPosition\": \"CC\", \"laterality\": \"R\"},\n                        {\"position\": 3, \"viewPosition\": \"CC\", \"laterality\": \"L\"}\n                    ]\n                }", "MG", "Mammography", 95 }
                });

            migrationBuilder.InsertData(
                table: "UserSettings",
                columns: new[] { "Id", "AnnotationColor", "DefaultLayout", "DefaultTool", "DefaultWindowPresets", "LastUpdated", "MeasurementColor", "ShowAnnotations", "ShowMeasurements", "ShowOverlay", "UserId" },
                values: new object[] { 1, "#00FF00", "1x1", "wwwc", "[\n                    {\"name\": \"CT Abdomen\", \"wc\": 40, \"ww\": 400},\n                    {\"name\": \"CT Bone\", \"wc\": 500, \"ww\": 2000},\n                    {\"name\": \"CT Brain\", \"wc\": 40, \"ww\": 80},\n                    {\"name\": \"CT Chest\", \"wc\": -600, \"ww\": 1500},\n                    {\"name\": \"CT Lung\", \"wc\": -400, \"ww\": 1500}\n                ]", new DateTime(2026, 1, 17, 18, 48, 39, 46, DateTimeKind.Utc).AddTicks(963), "#FFFF00", true, true, true, "default" });

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_InstanceId",
                table: "Annotations",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_HangingProtocols_Modality_BodyPart",
                table: "HangingProtocols",
                columns: new[] { "Modality", "BodyPart" });

            migrationBuilder.CreateIndex(
                name: "IX_Instances_SeriesId",
                table: "Instances",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Instances_SopInstanceUid",
                table: "Instances",
                column: "SopInstanceUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_InstanceId",
                table: "Measurements",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Series_Modality",
                table: "Series",
                column: "Modality");

            migrationBuilder.CreateIndex(
                name: "IX_Series_SeriesInstanceUid",
                table: "Series",
                column: "SeriesInstanceUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_StudyId",
                table: "Series",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_AccessionNumber",
                table: "Studies",
                column: "AccessionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_PatientId",
                table: "Studies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_PatientName",
                table: "Studies",
                column: "PatientName");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_StudyDate",
                table: "Studies",
                column: "StudyDate");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_StudyInstanceUid",
                table: "Studies",
                column: "StudyInstanceUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Annotations");

            migrationBuilder.DropTable(
                name: "HangingProtocols");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "Instances");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "Studies");
        }
    }
}
