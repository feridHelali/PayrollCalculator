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

  @Column("text", { name: "numero_table" })
  numeroTable!: string;

  @Column("text", { name: "type" })
  type!: string; // Horaire, Mensuelle

  @Column("text", { name: "consernedEmployee" })
  consernedEmployee!: string;

  @Column("date", { name: "beginning_date_of_application", nullable: false })
  beginningDateOfApplication!: Date;

  @Column("date", { name: "end_date_of_application", nullable: true })
  endDateOfApplication!: Date | null;

  @Column("json", { name: "degrees" })
  degrees!: {key: string, degree: number, ageOfWork: number}[];

  @Column("json", { name: "categories" })
  categories!: {key: string, label: string}[];

  @OneToMany(
    () => SalaryTableCell,
    (salaryTableCell) => salaryTableCell.salaryTable,
    { cascade: true ,eager: true}
  )
  salaryTableCells!: SalaryTableCell[];

  @ManyToOne(
    () => SectorialJointAgreement,
    (sectorialJointAgreement) => sectorialJointAgreement.salaryTables,
    { onDelete: 'CASCADE' }
  )
  @JoinColumn([
    { name: "agreementID"},
  ])
  agreement!: SectorialJointAgreement;

  @Column({ type: "integer", name: "agreementID", nullable: false })
  agreementId!: number;


}
