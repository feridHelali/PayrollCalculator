import {
  Column,
  Entity,
  JoinColumn,
  ManyToOne,
  OneToMany,
  PrimaryGeneratedColumn,
} from "typeorm";
import { SalaryTableCell } from "./SalaryTableCell";
import { SectorialJointAgreement } from "./SectorialJointAgreement";

@Entity("SalaryTable")
export class SalaryTable {
  @PrimaryGeneratedColumn({
    type: "integer",
    name: "salaryTableID",
  })
  salaryTableId!: number;

  @Column("integer", { name: "numero_table" })
  numeroTable!: number;

  @Column("text", { name: "type" })
  type!: string; // Horaire, Mensuelle

  @Column("text", { name: "consernedEmployee" })
  consernedEmployee!: string;

  @Column("date", { name: "biggining_date_of_application", nullable: false })
  bigginingDateOfApplication!: Date;

  @Column("date", { name: "end_date_of_application", nullable: true })
  endDateOfApplication!: Date | null;

  @Column("json", { name: "degrees" })
  degrees!: number[];

  @Column("json", { name: "workingAges" })
  workingAges!: number[];

  @Column("json", { name: "categories" })
  categories!: string[];

  @OneToMany(
    () => SalaryTableCell,
    (salaryTableCell) => salaryTableCell.salaryTable,
    { cascade: true }
  )
  salaryTableCells!: SalaryTableCell[];

  @ManyToOne(
    () => SectorialJointAgreement,
    (sectorialJointAgreement) => sectorialJointAgreement.salaryTables
  )
  @JoinColumn([
    { name: "agreementID", referencedColumnName: "sectorialJointAgreementId" },
  ])
  agreement!: SectorialJointAgreement;

  // Method to lookup salary for a given employee within a specific period
  getSalaryForEmployee(category: string, degree: number, workingAge: number, date: Date): number | null {
    if (date >= this.bigginingDateOfApplication && (!this.endDateOfApplication || date <= this.endDateOfApplication)) {
      const cell = this.salaryTableCells.find(cell =>
        cell.professionalCategory === category &&
        cell.professionalDegree === degree &&
        cell.workingAge === workingAge
      );
      return cell ? cell.salary : null;
    }
    return null;
  }

  // Method to update degree based on working age
  updateDegree(workingAge: number): void {
    this.salaryTableCells.forEach(cell => {
      cell.professionalDegree += Math.floor(workingAge / 5); // Example: increase degree every 5 years
    });
  }
}
