// src/contexts/SalaryTableContext.tsx
import React, { createContext, useContext, useState, ReactNode } from 'react';
import { ProfessionalCategory, ProfessionalDegree, SalaryTableProps } from '../../../../types/salaryTableProps'
import {v4 as uuid} from 'uuid'
import { labels } from '../../../arabic.labels';

interface SalaryTableContextProps {
  categories: ProfessionalCategory[];
  headers: ProfessionalDegree[];
  salaryTableCells: Record<string, Record<string, number>>;
  setCategories: React.Dispatch<React.SetStateAction<ProfessionalCategory[]>>;
  setHeaders: React.Dispatch<React.SetStateAction<ProfessionalDegree[]>>;
  setSalaryTableCells: React.Dispatch<React.SetStateAction<Record<string, Record<string, number>>>>;
}

const SalaryTableContext = createContext<SalaryTableContextProps | undefined>(undefined);

export const SalaryTableProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [categories, setCategories] = useState<ProfessionalCategory[]>([{ key: uuid().toString(), label: `1 - ${labels.category}` }]);
  const [headers, setHeaders] = useState<ProfessionalDegree[]>([{ key: uuid().toString(), degree: 1, ageOfWork: 1 }]);
  const [salaryTableCells, setSalaryTableCells] = useState<Record<string, Record<string, number>>>({});

  return (
    <SalaryTableContext.Provider value={{ categories, headers, salaryTableCells, setCategories, setHeaders, setSalaryTableCells }}>
      {children}
    </SalaryTableContext.Provider>
  );
};

export const useSalaryTableContext = () => {
  const context = useContext(SalaryTableContext);
  if (!context) {
    throw new Error('useSalaryTableContext must be used within a SalaryTableProvider');
  }
  return context;
};
