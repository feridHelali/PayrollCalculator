using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Infrastructure.Persistence;

/// <summary>
/// The sectoral joint agreements shipped with the product, seeded by the initial migration (EF <c>HasData</c>).
/// Id = display order, Code = stable slug, Name = official Arabic label. To change the list, edit it here and add a migration.
/// </summary>
internal static class AgreementSeed
{
    public static readonly (int Id, string Name, string Code)[] All =
    [
        (1, "الاتفاقية المشتركة القطاعية لصناعة الملابس المنسوجة والجاهزة", "vetements_tisses_et_prets_a_porter"),
        (2, "الاتفاقية المشتركة القطاعية للنسيج", "textile"),
        (3, "الاتفاقية المشتركة القطاعية للبناء والأشغال العامة", "batiment_et_travaux_publics"),
        (4, "الاتفاقية المشتركة القطاعية لصناعة تحويل البلاستيك", "transformation_du_plastique"),
        (5, "الاتفاقية المشتركة القطاعية للبنوك والمؤسسات المالية", "banques_et_institutions_financieres"),
        (6, "الاتفاقية المشتركة القطاعية لوكالات الأسفار", "agences_de_voyages"),
        (7, "الاتفاقية المشتركة القطاعية لمستخدمي الصيدليات", "employes_des_pharmacies"),
        (8, "الاتفاقية المشتركة القطاعية للتعليم الخاص", "enseignement_prive"),
        (9, "الاتفاقية المشتركة القطاعية للنزل السياحية والمؤسسات المشابهة", "hotels_touristiques"),
        (10, "الاتفاقية المشتركة القطاعية للتأمين", "assurances"),
        (11, "الاتفاقية المشتركة القطاعية للمسابك والتعدين والبناءات الميكانيكية", "fonderie_metallurgie_mecanique"),
        (12, "الاتفاقية المشتركة القطاعية لصناعة الكهرباء والألكترونيك", "electricite_et_electronique"),
        (13, "الاتفاقية المشتركة القطاعية للبناء المعدني", "construction_metallique"),
        (14, "الاتفاقية المشتركة القطاعية لصانعي ووكلاء السيارات", "constructeurs_et_concessionnaires_automobiles"),
        (15, "الاتفاقية المشتركة القطاعية لوكلاء التجهيزات الفلاحية والهندسة المدنية", "equipements_agricoles_et_genie_civil"),
        (16, "الاتفاقية المشتركة القطاعية للمصحات الخاصة", "cliniques_privees"),
        (17, "الاتفاقية المشتركة القطاعية للتصرف في النفايات الصلبة والسائلة", "gestion_des_dechets"),
        (18, "الاتفاقية المشتركة القطاعية لتحويل البلور والمرآة", "transformation_du_verre_et_miroiterie"),
        (19, "الاتفاقية المشتركة القطاعية للتجارة وتوزيع المواد والمنتوجات", "commerce_et_distribution"),
        (20, "الاتفاقية المشتركة القطاعية للمفرقعات", "explosifs_et_pyrotechnie"),
    ];

    /// <summary>
    /// The salary grid kinds of each agreement (the product owner's "sub_divisions"). The unit follows the code prefix:
    /// <c>horaire_</c> is hourly, anything else monthly (so <c>mensuel_horaire_execution</c>, hotels, is monthly: to confirm).
    /// </summary>
    public static readonly (int Id, int AgreementId, string Code, string Name, PayUnit Unit, int DefaultWeeklyHours)[] GridKinds =
    [
        (1, 1, "mensuel_cadres_maitrise", "جدول الأجور الخاص بالإطارات وأعوان التسيير (شهري)", PayUnit.Monthly, 48),
        (2, 1, "horaire_executants", "جدول الأجور الخاص بأعوان التنفيذ (ساعي)", PayUnit.Hourly, 48),
        (3, 2, "mensuel_cadres_maitrise", "جدول الأجور الخاص بالإطارات وأعوان التسيير", PayUnit.Monthly, 48),
        (4, 2, "horaire_ouvriers", "جدول الأجور الخاص بالعملة (ساعي)", PayUnit.Hourly, 48),
        (5, 3, "mensuel_administratif_technique", "جدول أجور الأعوان الإداريين والفنيين والإطارات", PayUnit.Monthly, 48),
        (6, 3, "horaire_ouvriers_qualifies", "جدول أجور عملة التنفيذ (ساعي)", PayUnit.Hourly, 48),
        (7, 4, "mensuel_cadres_maitrise", "جدول الأجور الخاص بالإطارات وأعوان التسيير", PayUnit.Monthly, 48),
        (8, 4, "horaire_executants", "جدول أجور أعوان التنفيذ والعملة", PayUnit.Hourly, 48),
        (9, 5, "mensuel_agents_titulaires", "جدول الأجور الموحد لأعوان البنوك والمؤسسات المالية (شهري)", PayUnit.Monthly, 40),
        (10, 6, "mensuel_toutes_categories", "جدول الأجور الأساسية الموحد لجميع الأصناف", PayUnit.Monthly, 48),
        (11, 7, "mensuel_diplomes_et_cadres", "جدول أجور الصيادلة المساعدين والإطارات", PayUnit.Monthly, 40),
        (12, 7, "mensuel_preparateurs_et_employes", "جدول أجور المحضرين وأعوان الصيدليات", PayUnit.Monthly, 40),
        (13, 8, "mensuel_enseignants", "جدول أجور سلك التدريس والتعليم", PayUnit.Monthly, 40),
        (14, 8, "mensuel_administratif", "جدول أجور السلك الإداري وأعوان الإحاطة", PayUnit.Monthly, 40),
        (15, 9, "mensuel_cadres_maitrise", "جدول أجور إطارات وأعوان تسيير المؤسسات السياحية", PayUnit.Monthly, 48),
        (16, 9, "mensuel_horaire_execution", "جدول أجور أعوان التنفيذ (حسب نظام العمل)", PayUnit.Monthly, 48),
        (17, 10, "mensuel_toutes_categories", "جدول الأجور الأساسية الموحد لأعوان شركات التأمين", PayUnit.Monthly, 40),
        (18, 11, "mensuel_cadres_maitrise", "جدول أجور الإطارات وأعوان التسيير (المعادن)", PayUnit.Monthly, 48),
        (19, 11, "horaire_ouvriers", "جدول أجور عملة التنفيذ بسلك التعدين (ساعي)", PayUnit.Hourly, 48),
        (20, 12, "mensuel_cadres_maitrise", "جدول الأجور الخاص بالإطارات وأعوان التسيير", PayUnit.Monthly, 48),
        (21, 12, "horaire_executants", "جدول أجور أعوان التنفيذ والعملة (ساعي)", PayUnit.Hourly, 48),
        (22, 13, "mensuel_cadres_maitrise", "جدول الأجور الخاص بالإطارات وأعوان التسيير", PayUnit.Monthly, 48),
        (23, 13, "horaire_ouvriers", "جدول أجور عملة التنفيذ (ساعي)", PayUnit.Hourly, 48),
        (24, 14, "mensuel_cadres_maitrise", "جدول الأجور الخاص بإطارات وأعوان تسيير قطاع السيارات", PayUnit.Monthly, 48),
        (25, 14, "horaire_ouvriers", "جدول أجور أعوان التنفيذ والعملة (ساعي)", PayUnit.Hourly, 48),
        (26, 15, "mensuel_cadres_maitrise", "جدول الأجور الخاص بالإطارات وأعوان التسيير", PayUnit.Monthly, 48),
        (27, 15, "horaire_executants", "جدول أجور عملة وأعوان التنفيذ (ساعي)", PayUnit.Hourly, 48),
        (28, 16, "mensuel_cadres_paramedical", "جدول أجور الإطارات الطبية الموازية والإداريين", PayUnit.Monthly, 40),
        (29, 16, "mensuel_execution_ouvriers", "جدول أجور أعوان التنفيذ والعملة بالمصحات", PayUnit.Monthly, 40),
        (30, 17, "mensuel_cadres_maitrise", "جدول أجور سلك الإطارات وأعوان الإشراف", PayUnit.Monthly, 48),
        (31, 17, "horaire_ouvriers_execution", "جدول أجور عملة التنفيذ الميدانيين (ساعي)", PayUnit.Hourly, 48),
        (32, 18, "mensuel_cadres_maitrise", "جدول أجور إطارات وأعوان تسيير قطاع البلور", PayUnit.Monthly, 48),
        (33, 18, "horaire_ouvriers", "جدول أجور عملة الفروع المهنية للتنفيذ (ساعي)", PayUnit.Hourly, 48),
        (34, 19, "mensuel_cadres_maitrise", "جدول الأجور الخاص بالإطارات وأعوان التسيير والتجارة", PayUnit.Monthly, 48),
        (35, 19, "mensuel_execution", "جدول الأجور الخاص بأعوان التنفيذ والخدمات", PayUnit.Monthly, 48),
        (36, 20, "mensuel_cadres_maitrise", "جدول أجور الإطارات الفنية والإدارية", PayUnit.Monthly, 48),
        (37, 20, "horaire_ouvriers_depots", "جدول أجور عملة ومناولى المواد المتفجرة (ساعي)", PayUnit.Hourly, 48),
    ];
}
