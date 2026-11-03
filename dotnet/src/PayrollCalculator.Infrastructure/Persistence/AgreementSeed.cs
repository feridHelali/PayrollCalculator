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
}
