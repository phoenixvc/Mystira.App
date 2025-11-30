import { TemplateInspector } from '../../../templates';

interface InfrastructureTemplatesTabProps {
  environment: string;
}

export default function InfrastructureTemplatesTab({ environment }: InfrastructureTemplatesTabProps) {
  return (
    <div className="h-full">
      <TemplateInspector environment={environment} />
    </div>
  );
}

