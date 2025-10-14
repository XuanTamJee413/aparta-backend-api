﻿using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Apartment
{
    public string ApartmentId { get; set; } = null!;

    public string? Code { get; set; }

    public string? Type { get; set; }

    public string? Status { get; set; }

    public string? BuildingId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public double? Area { get; set; }

    public virtual ICollection<ApartmentMember> ApartmentMembers { get; set; } = new List<ApartmentMember>();

    public virtual Building? Building { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<FeePeriod> FeePeriods { get; set; } = new List<FeePeriod>();

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public virtual ICollection<VisitLog> VisitLogs { get; set; } = new List<VisitLog>();
}
