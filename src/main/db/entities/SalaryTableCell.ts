import {
  Column,
  Entity,
  JoinColumn,
  ManyToOne,
  PrimaryGeneratedColumn,
} from "typeorm";
import { SalaryTable } from "./SalaryTable";

@Entity("SalaryTableCell")
export class SalaryTableCell {
  @PrimaryGeneratedColumn({ type: "integer", name: "id"})
  id!: number;

  @Column("text", { name: "professionalCategory" })
  professionalCategory!: string;

  @Column("text", { name: "prefessionalSubCategory", nullable: true })
  prefessionalSubCategory!: string | null;

  @Column("integer", { name: "ProfessionalDegree" })
  professionalDegree!: number;

  @Column("integer", { name: "WorkingAge" })
  workingAge!: number;

  @Column("real", { name: "salary" })
  salary!: number;

  @ManyToOne(() => SalaryTable, (salaryTable) => salaryTable.salaryTableCells)
  @JoinColumn([
    { name: "SalaryTableId", referencedColumnName: "salaryTableId" },
  ])
  salaryTable!: SalaryTable;


}
