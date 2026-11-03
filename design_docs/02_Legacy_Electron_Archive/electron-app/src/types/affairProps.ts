export interface affairProps {
    affairId?: number;
    affairNumber: number | string;
    title: string;
    claimant: string;
    startDateOfWork: string;
    endDateOfWork: string | null;
    professionalCategoryAtBegining: string;
    professionalDegreeAtBegining: number;
}