namespace SeeSharp.Application.Reports;

public record MonthlySummaryRow(int Year, int Month, decimal Income, decimal Expenses, decimal Net);

public record SummaryResponse(
    DateOnly From, DateOnly To, decimal TotalIncome, decimal TotalExpenses, decimal Net,
    IReadOnlyList<MonthlySummaryRow> Months);
