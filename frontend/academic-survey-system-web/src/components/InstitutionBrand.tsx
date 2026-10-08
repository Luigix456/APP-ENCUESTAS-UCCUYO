interface InstitutionBrandProps {
  compact?: boolean;
  subtitle?: string;
}

export function InstitutionBrand({ compact = false, subtitle = 'Sistema de Encuestas Académicas' }: InstitutionBrandProps) {
  return (
    <div className={`institution-brand${compact ? ' institution-brand--compact' : ''}`}>
      <div className="institution-brand__mark" aria-hidden="true">
        UC
      </div>
      <div className="institution-brand__copy">
        <strong>Universidad Católica de Cuyo</strong>
        <span>{subtitle}</span>
      </div>
    </div>
  );
}
