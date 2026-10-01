import { useAuth } from '../../auth/AuthProvider';
import {
  AcademicCatalogPermissionPanel,
  AcademicHomeCard,
  MANAGE_ACADEMIC_CATALOG_PERMISSION
} from './academicCatalogUi';

export function AcademicCatalogHomePage() {
  const auth = useAuth();

  if (!auth.hasPermission(MANAGE_ACADEMIC_CATALOG_PERMISSION)) {
    return <AcademicCatalogPermissionPanel />;
  }

  return (
    <section className="app-content academic-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Administración</p>
          <h2>Estructura académica</h2>
          <p>Prepará unidades académicas, carreras, materias, ciclos lectivos, docentes y asignaciones docentes.</p>
        </div>
      </header>

      <div className="catalog-home-grid">
        <AcademicHomeCard
          description="Facultades, departamentos o sedes que agrupan carreras."
          title="Unidades académicas"
          to="/app/academic/units"
        />
        <AcademicHomeCard
          description="Alta, edición y activación de carreras dentro de una unidad académica."
          title="Carreras"
          to="/app/academic/careers"
        />
        <AcademicHomeCard
          description="Materias vinculadas a una carrera, con año y período de dictado."
          title="Materias"
          to="/app/academic/subjects"
        />
        <AcademicHomeCard
          description="Ciclos lectivos activos e históricos con fechas de vigencia."
          title="Ciclos lectivos"
          to="/app/academic/cycles"
        />
        <AcademicHomeCard
          description="Docentes del catálogo académico y sus datos de contacto."
          title="Docentes"
          to="/app/academic/teachers"
        />
        <AcademicHomeCard
          description="Relación entre docente, materia, ciclo lectivo y rol docente."
          title="Asignaciones docentes"
          to="/app/academic/teacher-assignments"
        />
      </div>
    </section>
  );
}
