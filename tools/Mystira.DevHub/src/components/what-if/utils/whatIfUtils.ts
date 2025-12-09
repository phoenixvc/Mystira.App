export function getChangeTypeColor(changeType: string): string {
  switch (changeType) {
    case 'create':
      return 'text-green-700 bg-green-50 border-green-200';
    case 'modify':
      return 'text-yellow-700 bg-yellow-50 border-yellow-200';
    case 'delete':
      return 'text-red-700 bg-red-50 border-red-200';
    default:
      return 'text-gray-700 bg-gray-50 border-gray-200';
  }
}

export function getChangeTypeIcon(changeType: string): string {
  switch (changeType) {
    case 'create':
      return '+';
    case 'modify':
      return '~';
    case 'delete':
      return '-';
    default:
      return '=';
  }
}

