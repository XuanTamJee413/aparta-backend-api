using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Expense
{
    public string ExpenseId { get; set; } = null!;

    public string BuildingId { get; set; } = null!;

    public string TypeExpense { get; set; } = null!;

    public string? ExpenseDescription { get; set; }

    public decimal Price { get; set; }

    public DateOnly CreateDate { get; set; }

    public DateOnly? ActualPaymentDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Building Building { get; set; } = null!;
}
