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

  @Column("text", { name: "type" })
  type!: string;

  @Column("numeric", { name: "consernedEmployee" })
  consernedEmployee!: number;

  @OneToMany(
    () => SalaryTableCell,
    (salaryTableCell) => salaryTableCell.salaryTable
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
}
