using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Expense
{
    public string ExpenseId { get; set; } = null!;

    public DateOnly? CreateDate { get; set; }

    public DateOnly? ActualPaymentDate { get; set; }

    public string? TypeExpense { get; set; }

    public string? BuildingId { get; set; }

    public string? ExpenseDescription { get; set; }

    public double? Price { get; set; }

    public virtual Building? Building { get; set; }
}
