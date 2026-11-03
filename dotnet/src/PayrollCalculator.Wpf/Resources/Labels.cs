using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Wpf.Resources;

/// <summary>Arabic UI strings (ported from the Electron app's arabic.labels.ts). Used from XAML via x:Static.</summary>
public static class Labels
{
    public const string AppTitle = "حاسبة الأجور";
    public const string Agreements = "الاتفاقيات القطاعية المشتركة";
    public const string Agreement = "الاتفاقية القطاعية";
    public const string AgreementName = "اسم الاتفاقية القطاعية المشتركة";
    public const string Description = "بيانات على الاتفاقية";
    public const string AllAgreements = "كل الاتفاقيات";
    public const string SalaryTables = "جداول الأجور";
    public const string SalaryTable = "جدول الأجور";
    public const string TableNumber = "رقم الجدول";
    public const string SalaryTypeLabel = "نوع الجدول";
    public const string Monthly = "بالشهر";
    public const string Hourly = "بالساعة";
    public const string ConcernedEmployees = "الأعوان المعنيون";
    public const string StartDate = "تاريخ بداية التطبيق";
    public const string EndDate = "تاريخ نهاية التطبيق";
    public const string Category = "الصنف";
    public const string Categories = "الأصناف";
    public const string Degree = "الدرجة";
    public const string Degrees = "الدرجات";
    public const string YearsInDegree = "الأقدمية الفعلية (سنوات)";
    public const string AddCategory = "إضافة صنف";
    public const string AddDegree = "إضافة درجة";
    public const string SalaryGrid = "سلم الأجور";
    public const string Affairs = "القضايا";
    public const string Affair = "القضية";
    public const string AffairNumber = "عدد القضية";
    public const string Title = "الموضوع";
    public const string Claimant = "المدعي";
    public const string StartOfWork = "تاريخ بداية العمل";
    public const string EndOfWork = "تاريخ نهاية العمل";
    public const string InitialCategory = "الصنف عند الانتداب";
    public const string InitialDegree = "الدرجة عند الانتداب";
    public const string Timeline = "تطور الأجر الأساسي";
    public const string Period = "الشهر";
    public const string Seniority = "الأقدمية (أشهر)";
    public const string Amount = "الأجر";
    public const string Status = "الحالة";
    public const string TotalMonthly = "مجموع الأجور الشهرية";
    public const string UnresolvedMonths = "أشهر بدون أجر محدد";
    public const string ExportCsv = "تصدير CSV";
    public const string ImportLegacy = "استيراد بيانات التطبيق القديم";
    public const string New = "جديد";
    public const string Edit = "تحرير";
    public const string Save = "حفظ";
    public const string Delete = "حذف";
    public const string Back = "رجوع";
    public const string Refresh = "تحديث";
    public const string Saved = "تم الحفظ";
    public const string Deleted = "تم الحذف";
    public const string Loading = "تحميل...";
    public const string Ready = "جاهز";
    public const string Error = "خطأ";
    public const string ConfirmDelete = "هل تريد تأكيد الحذف؟";
    public const string ConfirmDeleteAgreement = "سيتم حذف الاتفاقية مع جداول الأجور والقضايا المرتبطة بها. هل تريد المتابعة؟";
    public const string DatesRequired = "يجب إدخال تاريخ البداية وتاريخ النهاية";
    public const string SelectAgreement = "يجب اختيار الاتفاقية";
    public const string ImportDone = "تم الاستيراد";
    public const string LegacyDbFilter = "SQLite (*.sqlite;*.db)|*.sqlite;*.db|*.*|*.*";
    public const string CsvFilter = "CSV (*.csv)|*.csv";

    public static string SalaryTypeText(SalaryType? type) => type switch
    {
        SalaryType.Monthly => Monthly,
        SalaryType.Hourly => Hourly,
        _ => string.Empty,
    };

    public static string IssueText(TimelineIssue issue) => issue switch
    {
        TimelineIssue.None => "✓",
        TimelineIssue.NoApplicableTable => "لا يوجد جدول أجور ساري المفعول",
        TimelineIssue.CategoryNotFound => "الصنف غير موجود في الجدول",
        TimelineIssue.DegreeNotFound => "الدرجة غير موجودة في الجدول",
        TimelineIssue.SalaryMissing => "الأجر غير مدرج في الجدول",
        _ => issue.ToString(),
    };
}
