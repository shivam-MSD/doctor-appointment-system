using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Infrastructure.Data;

var dbPath = @"d:\shivam\doctor-appointment-system\DoctorAppointmentSystem\DoctorAppointmentSystem\app.db";
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var db = new ApplicationDbContext(options);
var logs = db.ClinicAuditLogs.OrderByDescending(l => l.Timestamp).Take(5).ToList();

var result = "";
foreach (var log in logs)
{
    result += $"Log Action: {log.Action}\n";
    result += $"Old Data: {log.OldDataJson}\n";
    result += $"New Data: {log.NewDataJson}\n";
    result += "------------------------\n";
}

File.WriteAllText(@"d:\shivam\doctor-appointment-system\logs_dump.txt", result);
