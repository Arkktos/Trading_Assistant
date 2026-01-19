namespace Trading_Assistant.Service.Models;

public class MarketData
{
    public Asset Asset { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public long Volume { get; set; }
    public decimal DayChange { get; set; }
    public decimal WeekChange { get; set; }
    public decimal MonthChange { get; set; }
    public decimal DayChangePercent { get; set; }
    public decimal WeekChangePercent { get; set; }
    public decimal MonthChangePercent { get; set; }
    public decimal High52Week { get; set; }
    public decimal Low52Week { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<HistoricalPrice> History { get; set; } = new();
}

public class HistoricalPrice
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}
