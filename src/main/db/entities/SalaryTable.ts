import {
  Column,
  Entity,
  JoinColumn,
  ManyToOne,
  OneToMany,
  PrimaryGeneratedColumn,
} from "typeorm";
import { AgreementApplicationPeriods } from "./AgreementApplicationPeriods";
import { SalaryTableCell } from "./SalaryTableCell";

@Entity("SalaryTable")
export class SalaryTable {
  @PrimaryGeneratedColumn({
    type: "integer",
    name: "salaryTableID"
  })
  salaryTableId!: number;

  @Column("text", { name: "type" })
  type!: string;

  @Column("numeric", { name: "consernedEmployee" })
  consernedEmployee!: number;

  @ManyToOne(
    () => AgreementApplicationPeriods,
    (agreementApplicationPeriods) => agreementApplicationPeriods.salaryTables
  )
  @JoinColumn([
    {
      name: "agreementApplicationPeriodsID",
      referencedColumnName: "agreementApplicationPeriodsId",
    },
  ])
  agreementApplicationPeriods!: AgreementApplicationPeriods;

  @OneToMany(
    () => SalaryTableCell,
    (salaryTableCell) => salaryTableCell.salaryTable
  )
  salaryTableCells!: SalaryTableCell[];

 
}
