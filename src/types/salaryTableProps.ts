
import { v4 as uuid } from 'uuid';
import { sectorialJointAgreementProps } from "./sectorialAgreementProps";

export interface SalaryTableProps {
    salaryTableId?: number;
    numeroTable: string;
    type: string;
    consernedEmployee: string;
    beginningDateOfApplication: string;
    endDateOfApplication: string | null;
    degrees: ProfessionalDegree[];
    categories: ProfessionalCategory[];
    salaryTableCells?: SalaryTableCell[];
    agreement?: sectorialJointAgreementProps;
    agreementId?: number;
}

export interface ProfessionalDegree{
    key: string;
    degree: number;
    ageOfWork: number;
}

export interface ProfessionalCategory {
    key: string;
    label: string;
}

export interface SalaryTableCell {
    professionalCategory: string;
    professionalSubCategory?: string | null;
    professionalDegree: string;
    salary: number;
    salaryTableId?: number;
}