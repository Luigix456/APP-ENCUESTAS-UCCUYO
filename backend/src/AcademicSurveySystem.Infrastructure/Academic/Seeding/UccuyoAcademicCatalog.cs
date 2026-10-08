using AcademicSurveySystem.Domain.Academic.Enums;

namespace AcademicSurveySystem.Infrastructure.Academic.Seeding;

public static class UccuyoAcademicCatalog
{
    public static IReadOnlyList<AcademicUnitSeed> Units { get; } =
    [
        new AcademicUnitSeed(
            "economicas",
            "Facultad de Cs. Económicas y Empresariales",
            [
                Undergraduate("contador-publico", "Contador Público (Presencial)"),
                Undergraduate("lic-administracion", "Lic. en Administración"),
                Undergraduate("lic-comercio-internacional", "Lic. en Comercio Internacional"),
                Undergraduate("lic-economia", "Lic. en Economía"),
                Undergraduate("lic-turismo-hoteleria", "Lic. en Turismo y Hotelería"),
                Undergraduate("tuds", "Tec. Univ. en Desarrollo de Software"),
                Postgraduate("eco-maestria-adm-estrategica-negocios", "Maestría en Adm. estratégica de Negocios"),
                Postgraduate("eco-esp-sindicatura-concursal", "Esp. en Sindicatura Concursal"),
                Postgraduate("eco-esp-contabilidad-superior-auditoria", "Esp. en Contabilidad Superior y Auditoría"),
                Undergraduate("lic-marketing", "Licenciatura en Marketing")
            ]),
        new AcademicUnitSeed(
            "medicas",
            "Facultad de Cs. Médicas",
            [
                Undergraduate("med-medicina", "Medicina"),
                Undergraduate("med-lic-nutricion", "Lic. en Nutrición"),
                Undergraduate("med-lic-kinesiologia-fisiatria", "Lic. en Kinesiología y Fisiatría"),
                Undergraduate("med-lic-enfermeria", "Lic en Enfermería"),
                Undergraduate("med-lic-terapia-ocupacional", "Lic. en Terapia Ocupacional"),
                Postgraduate("med-doctorado-ciencias-biomedicas", "Doctorado en Cs. Biomédicas"),
                Undergraduate("med-lic-obstetricia", "Lic. en Obstetricia"),
                Undergraduate("med-lic-fonoaudiologia", "Lic. en Fonoaudiología"),
                Undergraduate("med-tec-podologia", "Tec. Univ. en Podología")
            ]),
        new AcademicUnitSeed(
            "derecho-sociales",
            "Facultad de Derecho y Cs. Sociales",
            [
                Undergraduate("der-abogacia", "Abogacía (Presencial)"),
                Undergraduate("der-corredor-comercio-martillero", "Corr. de comercio e inm., martillero público"),
                Undergraduate("der-lic-comunicacion-social", "Lic. en Comunicación Social (Presencial)"),
                Undergraduate("der-lic-relaciones-internacionales", "Lic. en Relaciones Internacionales (Presencial)"),
                Postgraduate("der-esp-derecho-procesal-civil", "Esp. en Derecho Procesal Civil"),
                Postgraduate("der-esp-magistratura-gestion-judicial", "Esp. en Magistratura y Gestión Judicial"),
                Postgraduate("der-especializacion-derecho-danos", "Especialización en Derecho de Daños"),
                Postgraduate("der-maestria-gestion-negocio-minero", "Maestría en Gestión del Negocio Minero"),
                Postgraduate("der-maestria-derecho-empresario", "Maestría en Derecho Empresario"),
                Postgraduate("der-maestria-derecho-administrativo-economia", "Maestría en Derecho Administrativo de la Economía")
            ]),
        new AcademicUnitSeed(
            "quimicas-tecnologicas",
            "Facultad de Cs. Químicas y Tecnológicas",
            [
                Undergraduate("qt-farmacia", "Farmacia"),
                Undergraduate("qt-lic-bioquimica", "Lic. en Bioquímica"),
                Undergraduate("qt-lic-enologia", "Lic. en Enología (Sede San Juan)"),
                Undergraduate("qt-lic-tecnologia-alimentos", "Lic. en Tecnología de los Alimentos"),
                Undergraduate("qt-tec-esterilizacion", "Tec. Universitaria en Esterilización"),
                Undergraduate("qt-sommelier", "Sommelier (San Juan)"),
                Undergraduate("qt-lic-seguridad-salud-ocupacional", "Lic. en Seguridad y Salud Ocupacional"),
                Undergraduate("qt-lic-gestion-gastronomica", "Lic. en Gestión Gastronómica"),
                Postgraduate("qt-doctorado-ciencias-biomedicas", "Doctorado en Ciencias Biomédicas"),
                Undergraduate("qt-lic-seguridad-salud-ocupacional-ccc", "Lic. en Seguridad y Salud Ocupacional - CCC")
            ]),
        new AcademicUnitSeed(
            "filosofia-humanidades",
            "Facultad de Filosofía y Humanidades",
            [
                Undergraduate("fh-lic-psicologia", "Lic en Psicología"),
                Undergraduate("fh-lic-recursos-humanos", "Lic. en Recursos Humanos"),
                Undergraduate("fh-profesorado-filosofia", "Profesorado en Filosofía"),
                Postgraduate("fh-maestria-nuevas-tecnologias-comunicacion", "Maestría Gestión de Nuevas Tecnologías en Comunicación"),
                Postgraduate("fh-doctorado-estudios-patristicos", "Doctorado en Estudios Patrísticos"),
                Postgraduate("fh-maestria-evaluacion-psicologica-tcc", "Maestría en Evaluación Psicológica con Mención en TCC"),
                Postgraduate("fh-esp-alta-gerencia-publica", "Especialización en Alta Gerencia Pública"),
                Postgraduate("fh-esp-psicologia-ninez-adolescencia", "Especialización en psicología de la Niñez y la Adolescencia")
            ]),
        new AcademicUnitSeed(
            "educacion",
            "Facultad de Educación",
            [
                Undergraduate("edu-lic-psicopedagogia", "Lic. en Psicopedagogía"),
                Undergraduate("edu-lic-psicomotricidad", "Lic. en Psicomotricidad"),
                Postgraduate("edu-doctorado-educacion", "Doctorado en Educación"),
                Undergraduate("edu-tec-guia-montana", "Tec. en Guía de Montaña")
            ]),
        new AcademicUnitSeed(
            "seguridad",
            "Escuela de Seguridad",
            [
                Course("seg-diplomatura-balistica", "Diplomatura en Balística"),
                Course("seg-diplomatura-papiloscopia", "Diplomatura en Papiloscopía - Huellas y Rastros"),
                Course("seg-diplomatura-documentoscopia", "Diplomatura en Documentoscopía"),
                Course("seg-diplomatura-seguridad-patrimonial", "Diplomatura en Seguridad Patrimonial en Entornos Industriales y Mineros")
            ]),
        new AcademicUnitSeed(
            "institutos-superiores",
            "Institutos Superiores de Formación",
            [
                Undergraduate("isf-prof-educacion-primaria-sm", "Profesorado en Educación Primaria (SM)"),
                Undergraduate("isf-prof-educacion-fisica", "Profesorado de Educación Física"),
                Undergraduate("isf-prof-ciencias-sagradas", "Profesorado de Ciencias Sagradas"),
                Undergraduate("isf-prof-educacion-especial", "Profesorado de Educación Especial"),
                Undergraduate("isf-prof-educacion-inicial", "Profesorado de Educación Inicial"),
                Undergraduate("isf-prof-educacion-primaria-sb", "Profesorado en Educación Primaria (SB)")
            ]),
        new AcademicUnitSeed(
            "cultura-religiosa-pastoral",
            "Escuela de Cultura Religiosa y Pastoral",
            [
                Undergraduate("crp-lic-ciencias-sagradas-ccc", "Licenciatura en Ciencias Sagradas (CCC)")
            ])
    ];

    private static CareerSeed Undergraduate(string code, string name) =>
        new(code, name, CareerType.Undergraduate);

    private static CareerSeed Postgraduate(string code, string name) =>
        new(code, name, CareerType.Postgraduate);

    private static CareerSeed Course(string code, string name) =>
        new(code, name, CareerType.Course);
}

public sealed record AcademicUnitSeed(
    string Code,
    string Name,
    IReadOnlyList<CareerSeed> Careers);

public sealed record CareerSeed(
    string Code,
    string Name,
    CareerType Type);
