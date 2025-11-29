import type { TemplateConfig } from '../../../types';

interface ReadyToDeployBannerProps {
  templates: TemplateConfig[];
}

export function ReadyToDeployBanner({ templates }: ReadyToDeployBannerProps) {
  const selectedTemplates = templates.filter(t => t.selected);

  if (selectedTemplates.length === 0) return null;

  return (
    <div className="mb-8 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
      <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-200 mb-2">
        Ready to Deploy
      </h3>
      <p className="text-sm text-blue-800 dark:text-blue-300 mb-3">
        Preview completed. The following infrastructure will be deployed based on your selected templates:
      </p>
      <div className="flex flex-wrap gap-2">
        {selectedTemplates.map(template => (
          <span
            key={template.id}
            className="px-3 py-1.5 bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-200 rounded-md text-sm font-medium"
          >
            {template.name}
          </span>
        ))}
      </div>
      <p className="text-xs text-blue-700 dark:text-blue-400 mt-3">
        Note: Cosmos DB nested resources (databases/containers) couldn't be previewed due to Azure limitations, but they will be deployed correctly.
      </p>
    </div>
  );
}

