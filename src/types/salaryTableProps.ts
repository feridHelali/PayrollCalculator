import { UUID } from "crypto";
import { sectorialJointAgreementProps } from "./sectorialAgreementProps";

export interface SalaryTableProps {
    salaryTableId?: number;
    numeroTable: string;
    type: string;
    consernedEmployee: string;
    beginningDateOfApplication: string;
    endDateOfApplication: string | null;
    degrees: number[];
    workingAges: number[];
    categories: string[];
    salaryTableCells?: SalaryTableCell[];
    agreement?: sectorialJointAgreementProps;
    agreementId?: number;
}

export interface SalaryTableCell {
    id: number;
    professionalCategory: string;
    prefessionalSubCategory?: string | null;
    professionalDegree: number;
    workingAge: number;
    salary: number;
    salaryTable: SalaryTableProps;
}