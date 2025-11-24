import { useEffect, useState } from 'react';
import { ServiceConfig } from './types';

export function useServiceOrder(serviceConfigs: ServiceConfig[]) {
  const [orderedServices, setOrderedServices] = useState<ServiceConfig[]>(() => {
    const saved = localStorage.getItem('serviceOrder');
    if (saved) {
      try {
        const order = JSON.parse(saved) as string[];
        // Reorder based on saved order, keeping any new services at the end
        const ordered = order
          .map(name => serviceConfigs.find(s => s.name === name))
          .filter((s): s is ServiceConfig => s !== undefined);
        const newServices = serviceConfigs.filter(s => !order.includes(s.name));
        return [...ordered, ...newServices];
      } catch {
        // Fall through to default
      }
    }
    return serviceConfigs;
  });

  useEffect(() => {
    // Update order when serviceConfigs change (new services added)
    const saved = localStorage.getItem('serviceOrder');
    if (saved) {
      try {
        const order = JSON.parse(saved) as string[];
        const ordered = order
          .map(name => serviceConfigs.find(s => s.name === name))
          .filter((s): s is ServiceConfig => s !== undefined);
        const newServices = serviceConfigs.filter(s => !order.includes(s.name));
        setOrderedServices([...ordered, ...newServices]);
      } catch {
        setOrderedServices(serviceConfigs);
      }
    } else {
      setOrderedServices(serviceConfigs);
    }
  }, [serviceConfigs]);

  const reorderServices = (fromIndex: number, toIndex: number) => {
    setOrderedServices(prev => {
      const newOrder = [...prev];
      const [removed] = newOrder.splice(fromIndex, 1);
      newOrder.splice(toIndex, 0, removed);
      
      // Save order to localStorage
      const order = newOrder.map(s => s.name);
      localStorage.setItem('serviceOrder', JSON.stringify(order));
      
      return newOrder;
    });
  };

  return { orderedServices, reorderServices };
}

