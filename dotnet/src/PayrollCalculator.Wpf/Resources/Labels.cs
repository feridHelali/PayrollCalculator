using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Domain.Users;
using PayrollCalculator.Application.Common;
using PayrollCalculator.Application.Licensing;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Wpf.Services;

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
    public const string Allowances = "المنح والمنافع";
    public const string Ledger = "الكشف الشهري للأجر والمنح";
    public const string Calculate = "احتساب";
    public const string LedgerCalculated = "تم احتساب الكشف";
    public const string PrimesForEmployee = "المنح المستحقة لهذا العامل";
    public const string NoPrimesForAgreement = "لا توجد منح مسجلة لهذه الاتفاقية (قائمة المنح والمنافع)";
    public const string GrandTotal = "المجموع العام";
    public const string BaseSalary = "الأجر الأساسي";
    public const string Month = "الشهر";
    public const string GridRate = "مقدار الجدول";
    public const string Proration = "نسبة الشهر";
    public const string MonthTotal = "مجموع الشهر";
    public const string Allowance = "المنحة";
    public const string AllowanceName = "اسم المنحة";
    public const string AllowanceUnitLabel = "طريقة الاحتساب";
    public const string AppliesTo = "المنتفعون";
    public const string Conditions = "شروط الإسناد";
    public const string Rates = "المبالغ حسب تاريخ التطبيق";
    public const string AddRate = "إضافة مبلغ";
    public const string EffectiveFrom = "بداية من";
    public const string RateValue = "المبلغ أو النسبة";
    public const string RateSource = "المرجع (الرائد الرسمي، الصفحة، الفصل)";
    public const string CurrentValue = "المبلغ الحالي";
    public const string FirstDate = "منذ";
    public const string RateCount = "عدد المبالغ";
    public const string RateIncomplete = "يجب إدخال التاريخ والمبلغ في كل سطر";
    public const string SalaryTable = "جدول الأجور";
    public const string TableNumber = "رقم الجدول";
    public const string SalaryTypeLabel = "نوع الجدول";
    public const string Monthly = "بالشهر";
    public const string Hourly = "بالساعة";
    public const string GridKind = "نوع الجدول";
    public const string WeeklyHours = "نظام العمل (ساعة في الأسبوع)";
    public const string ImportCsv = "استيراد من ملف CSV";
    public const string SeededReadOnly = "بيانات مرفقة بالبرنامج (مصدرها الرائد الرسمي): للاطلاع فقط";
    public const string CsvFilter = "CSV (*.csv)|*.csv";
    public const string ConfirmReplaceGrid = "سيتم تعويض الأصناف والدرجات الحالية بمحتوى الملف. هل تريد المواصلة؟";
    public const string GridImported = "تم استيراد الجدول: يرجى مراجعته قبل الحفظ";
    public const string DefaultRegime = "نظام العمل الافتراضي";
    public const string HoursPerWeek = "ساعة في الأسبوع";
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

    // Sign-in and users
    public const string Publisher = "AlfaComputers";
    public const string Welcome = "مرحبا بك";
    public const string SignInPrompt = "سجّل الدخول للمتابعة";
    public const string SignIn = "دخول";
    public const string Activation = "تفعيل البرنامج";
    public const string ActivationPrompt = "أدخل رقم الترخيص الذي سُلّم إليك مع البرنامج. يُطلب مرة واحدة على هذا الجهاز.";
    public const string SerialNumber = "رقم الترخيص";
    public const string Activate = "تفعيل";
    public const string SerialInvalidFormat = "رقم الترخيص يتكون من 20 حرفا ورقما (XXXXX-XXXXX-XXXXX-XXXXX): يرجى التثبت منه";
    public const string SerialUnknown = "رقم الترخيص غير صالح لهذه النسخة";
    public const string License = "الترخيص";
    public const string NotActivated = "غير مفعل";
    public const string DataFallback = "تعذرت الكتابة في مجلد البرنامج: البيانات محفوظة في مجلد المستخدم";
    public const string SignOut = "تسجيل الخروج";
    public const string Quit = "إغلاق";
    public const string UserName = "اسم المستخدم";
    public const string Password = "كلمة المرور";
    public const string CurrentPassword = "كلمة المرور الحالية (المؤقتة)";
    public const string NewPassword = "كلمة المرور الجديدة";
    public const string ConfirmPassword = "تأكيد كلمة المرور الجديدة";
    public const string ChangePassword = "تغيير كلمة المرور";
    public const string ChangePasswordPrompt = "كلمة المرور مؤقتة: اختر كلمة مرور جديدة قبل المتابعة";
    public const string PasswordRules = "8 أحرف على الأقل، وثلاثة أنواع من: حروف صغيرة، حروف كبيرة، أرقام، رموز";
    public const string Users = "المستخدمون";
    public const string DisplayName = "الاسم الكامل";
    public const string AddUser = "إضافة مستخدم";
    public const string ResetPassword = "إعادة تعيين كلمة المرور";
    public const string TemporaryPassword = "كلمة المرور المؤقتة";
    public const string TemporaryPasswordHint = "يُبلَّغ بها المستخدم، وسيُطلب منه تغييرها عند أول دخول";
    public const string Generate = "توليد";
    public const string LastSignIn = "آخر دخول";
    public const string UserAdded = "تمت إضافة المستخدم";
    public const string PasswordWasReset = "تمت إعادة تعيين كلمة المرور";
    public const string SelectUser = "يجب اختيار مستخدم من القائمة";
    public const string ConfirmReset = "سيُطلب من المستخدم اختيار كلمة مرور جديدة عند دخوله القادم. هل تريد المتابعة؟";
    public const string FirstRunHint = "أول تشغيل، الحساب الأولي:";

    /// <summary>Shown in its own left-to-right block: inside Arabic text "Admin@2026" would render as "2026@Admin".</summary>
    public const string FirstRunCredentials = "admin  /  Admin@2026";

    public const string InvalidCredentials = "اسم المستخدم أو كلمة المرور غير صحيحة";
    public const string AccountLocked = "الحساب مقفل مؤقتا بعد عدة محاولات فاشلة. أعد المحاولة بعد 15 دقيقة";
    public const string UserNameInvalid = "اسم المستخدم غير صالح: حروف لاتينية أو أرقام أو . - _ (32 حرفا على الأقصى)";
    public const string DisplayNameRequired = "يجب إدخال الاسم الكامل";
    public const string UserNameTaken = "اسم المستخدم مستعمل";
    public const string PasswordRejected = "كلمة المرور لا تستجيب للشروط:";
    public const string PasswordTooShort = "8 أحرف على الأقل";
    public const string PasswordTooSimple = "ثلاثة أنواع على الأقل من: حروف صغيرة، حروف كبيرة، أرقام، رموز";
    public const string PasswordSameAsUserName = "يجب أن تختلف عن اسم المستخدم";
    public const string WrongCurrentPassword = "كلمة المرور الحالية غير صحيحة";
    public const string PasswordUnchanged = "يجب أن تختلف كلمة المرور الجديدة عن الحالية";
    public const string PasswordConfirmationMismatch = "تأكيد كلمة المرور لا يطابق كلمة المرور الجديدة";

    // About and splash
    public const string About = "حول البرنامج";
    public const string AppDescription = "تحديد الأجور الأساسية على فترات طويلة حسب الاتفاقيات القطاعية المشتركة";
    public const string Version = "الإصدار";
    public const string Idea = "الفكرة";
    public const string IdeaBy = "Lazhar Lataoui";
    public const string Development = "التطوير";
    public const string DevelopedBy = "Ferid HELALI — AlfaComputers";
    public const string AssistedBy = "طُوِّر بمساعدة Claude Code";
    public const string Runtime = "بيئة التشغيل";
    public const string OperatingSystem = "نظام التشغيل";
    public const string Database = "قاعدة البيانات";
    public const string CopyInfo = "نسخ المعلومات";
    public const string InfoCopied = "تم نسخ المعلومات";
    public const string SupportInfo = "معلومات للدعم الفني";

    public static string StartupStageText(StartupStage stage) => stage switch
    {
        StartupStage.Starting => "جاري التشغيل...",
        StartupStage.Database => "تهيئة قاعدة البيانات...",
        StartupStage.Accounts => "تهيئة الحسابات...",
        StartupStage.LegacyImport => "استيراد بيانات التطبيق القديم...",
        StartupStage.Ready => "جاهز",
        _ => stage.ToString(),
    };

    // Errors
    public const string NotFound = "العنصر المطلوب غير موجود، ربما حُذف. حدّث القائمة ثم أعد المحاولة";
    public const string PasswordChanged = "تم تغيير كلمة المرور";
    public const string ChangeMyPassword = "تغيير كلمة المرور";
    public const string MyCurrentPassword = "كلمة المرور الحالية";
    public const string DatePlaceholder = "يوم/شهر/سنة";

    /// <summary>The message shown for any expected error, whatever layer raised it.</summary>
    public static string ErrorText(Exception error) => error switch
    {
        UserException user => UserErrorText(user),
        DomainException domain => DomainErrorText(domain),
        NotFoundException => NotFound,
        Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: { } inner } => inner.Message,
        null => string.Empty,
        _ => error.Message,
    };

    public static string DomainErrorText(DomainException error)
    {
        ArgumentNullException.ThrowIfNull(error);
        var field = FieldText(error.Field);
        var detail = error.Detail is { Length: > 0 } d ? $" «{d}»" : string.Empty;
        return error.Error switch
        {
            DomainError.Required => $"يجب إدخال {field}",
            DomainError.MustBePositive => $"{field}: يجب أن تكون القيمة أكبر من صفر",
            DomainError.CannotBeNegative => $"{field}: لا يمكن أن تكون القيمة سالبة",
            DomainError.EndBeforeStart => "تاريخ النهاية لا يمكن أن يسبق تاريخ البداية",
            DomainError.PeriodRequired => $"يجب تحديد {field}",
            DomainError.PeriodMustEnd => $"{field}: يجب إدخال تاريخ النهاية",
            DomainError.OpenEndedRange => "لا يمكن حساب الأشهر لفترة بدون تاريخ نهاية",
            DomainError.OutOfRange => $"{field}: قيمة خارج المجال المسموح{detail}",
            DomainError.Duplicate => $"{field}{detail} موجود مسبقا",
            DomainError.Unknown => $"{field}{detail} غير موجود",
            DomainError.IdentityAlreadyAssigned => "المعرّف محدد مسبقا",
            DomainError.TableNotInAgreement => "جدول الأجور لا ينتمي إلى الاتفاقية المختارة",
            DomainError.TableAgreementLocked => "لا يمكن نقل جدول الأجور إلى اتفاقية أخرى",
            DomainError.CareerMustStartWithHiring => "يجب أن يبدأ المسار المهني بالانتداب",
            DomainError.OutsideCareer => "حدث خارج فترة المسار المهني (بعد انتهاء العمل)",
            DomainError.NotApplicable => $"{field}{detail}: لا يمكن تطبيق هذا التعديل",
            DomainError.Undeclared => $"{field}{detail} غير معرّف في هذا التاريخ",
            DomainError.InvalidStatusTransition => $"{field}{detail}: لا يمكن الانتقال إلى هذه الحالة",
            DomainError.ImmutableOnceApproved => $"{field}{detail}: لا يمكن تعديل حدث مصادق عليه، يجب سحبه وإدخال حدث جديد",
            DomainError.InvalidImportFile => $"الملف غير صالح: يرجى مراجعة السطر{detail}",
            DomainError.ReadOnlySeed => $"{field}{detail}: هذه البيانات مرفقة بالبرنامج ولا يمكن تعديلها أو حذفها",
            DomainError.IncompleteSnapshot => $"{field}{detail}: بعض الأحداث المحفوظة في هذه النسخة لم تعد موجودة",
            _ => "عملية غير مسموح بها: " + error.Message,
        };
    }

    public static string FieldText(DomainField field) => field switch
    {
        DomainField.Id => "المعرّف",
        DomainField.AgreementName => AgreementName,
        DomainField.Agreement => Agreement,
        DomainField.AffairNumber => AffairNumber,
        DomainField.Claimant => Claimant,
        DomainField.EmploymentPeriod => "فترة العمل",
        DomainField.ProfessionalCategory => Category,
        DomainField.ProfessionalDegree => Degree,
        DomainField.TableNumber => TableNumber,
        DomainField.ApplicationPeriod => "فترة التطبيق",
        DomainField.CategoryLabel => Category,
        DomainField.CategoryKey => "مفتاح الصنف",
        DomainField.Degree => Degree,
        DomainField.DegreeKey => "مفتاح الدرجة",
        DomainField.YearsInDegree => YearsInDegree,
        DomainField.Salary => Amount,
        DomainField.Month => "الشهر",
        DomainField.Year => "السنة",
        DomainField.PasswordHash => Password,
        DomainField.SalaryTable => SalaryTable,
        DomainField.Amount => Amount,
        DomainField.Percentage => "النسبة المائوية",
        DomainField.HoursPerMonth => "عدد الساعات في الشهر",
        DomainField.Regime => "نظام العمل",
        DomainField.GridLine => "سلسلة جداول الأجور",
        DomainField.CategoryCode => "رمز الصنف",
        DomainField.Component => "عنصر الأجر",
        DomainField.SourceNumber => "عدد النص",
        DomainField.SourceTitle => "عنوان النص",
        DomainField.JortIssue => "عدد الرائد الرسمي",
        DomainField.LegalSource => "النص القانوني",
        DomainField.Page => "الصفحة",
        DomainField.MonthsInDegree => "عدد الأشهر في الدرجة",
        DomainField.GridCell => "خانة الجدول",
        DomainField.CareerEvent => "حدث المسار المهني",
        DomainField.RuleEvent => "الحدث التنظيمي",
        DomainField.Reviewer => "المراجع",
        DomainField.LedgerSnapshot => "نسخة السجل",
        DomainField.Affair => Affair,
        DomainField.DaysWorked => "عدد أيام العمل",
        DomainField.Unit => "وحدة الأجر",
        DomainField.AgreementCode => "رمز الاتفاقية",
        DomainField.AllowanceName => AllowanceName,
        DomainField.EffectiveDate => "تاريخ التطبيق",
        DomainField.WeeklyHours => WeeklyHours,
        DomainField.ImportFile => "الملف",
        _ => string.Empty,
    };

    public static string ActivationOutcomeText(ActivationOutcome outcome) => outcome switch
    {
        ActivationOutcome.Activated => string.Empty,
        ActivationOutcome.InvalidFormat => SerialInvalidFormat,
        _ => SerialUnknown,
    };

    public static string SignInOutcomeText(SignInOutcome outcome) => outcome switch
    {
        SignInOutcome.LockedOut => AccountLocked,
        SignInOutcome.MustChangePassword => ChangePasswordPrompt,
        SignInOutcome.Succeeded => string.Empty,
        _ => InvalidCredentials,
    };

    public static string UserErrorText(UserException error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error.Error switch
        {
            UserError.UserNameInvalid => UserNameInvalid,
            UserError.DisplayNameRequired => DisplayNameRequired,
            UserError.UserNameTaken => UserNameTaken,
            UserError.PasswordRejected => PasswordRejected + "\n" + string.Join("\n", error.Violations.Select(v => "• " + ViolationText(v))),
            UserError.WrongCurrentPassword => WrongCurrentPassword,
            UserError.PasswordUnchanged => PasswordUnchanged,
            UserError.AccountLocked => AccountLocked,
            UserError.PasswordConfirmationMismatch => PasswordConfirmationMismatch,
            _ => error.Message,
        };
    }

    public static string ViolationText(PasswordViolation violation) => violation switch
    {
        PasswordViolation.TooShort => PasswordTooShort,
        PasswordViolation.TooSimple => PasswordTooSimple,
        PasswordViolation.SameAsUserName => PasswordSameAsUserName,
        _ => violation.ToString(),
    };

    public static string UserStatusText(UserStatus status) => status switch
    {
        UserStatus.Active => "نشط",
        UserStatus.MustChangePassword => "في انتظار تغيير كلمة المرور",
        UserStatus.Locked => "مقفل مؤقتا",
        _ => status.ToString(),
    };

    public static string LedgerIssueText(LedgerIssue issue) => issue switch
    {
        LedgerIssue.None => "✓",
        LedgerIssue.NoGridInForce => "لا يوجد جدول أجور ساري المفعول",
        LedgerIssue.CategoryNotInGrid => "الصنف غير موجود في الجدول",
        LedgerIssue.SalaryMissing => "لا يوجد مبلغ لهذا الصنف وهذه الدرجة",
        LedgerIssue.RegimeUnknown => "نظام العمل غير معروف (عدد الساعات)",
        LedgerIssue.BaseMissing => "الأجر الأساسي غير معروف لاحتساب النسبة",
        _ => issue.ToString(),
    };

    public static string AllowanceUnitText(AllowanceUnit? unit) => unit switch
    {
        AllowanceUnit.PerMonth => "مبلغ شهري (د)",
        AllowanceUnit.PerHour => "عن كل ساعة عمل (د)",
        AllowanceUnit.PerDayWorked => "عن كل يوم عمل (د)",
        AllowanceUnit.PercentOfBase => "نسبة من الأجر الأساسي (%)",
        _ => string.Empty,
    };

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
