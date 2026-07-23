using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.Models;

public enum ChartPeriod
{
    [Display(Name = "1 Day")]
    OneDay,

    [Display(Name = "1 Week")]
    OneWeek,

    [Display(Name = "1 Month")]
    OneMonth,

    [Display(Name = "3 Months")]
    ThreeMonths,

    [Display(Name = "6 Months")]
    SixMonths,

    [Display(Name = "1 Year")]
    OneYear,

    [Display(Name = "2 Years")]
    TwoYears,

    [Display(Name = "5 Years")]
    FiveYears
}
