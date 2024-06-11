import {
  Column,
  Entity,
  JoinColumn,
  ManyToOne,
  OneToMany,
  PrimaryGeneratedColumn,
} from "typeorm";
import { SectorialJointAgreement } from "./SectorialJointAgreement";
import { SalaryTable } from "./SalaryTable";

@Entity("AgreementApplicationPeriods")
export class AgreementApplicationPeriods {
  @PrimaryGeneratedColumn({
    type: "integer",
    name: "agreementApplicationPeriodsID"
  })
  agreementApplicationPeriodsId!: number;

  @Column("date", { name: "start_date_of_application" })
  startDateOfApplication!: string;

  @Column("date", { name: "end_date_of_application", nullable: true })
  endDateOfApplication!: string | null;

  @Column("text", { name: "description", nullable: true })
  description!: string | null;

  @ManyToOne(
    () => SectorialJointAgreement,
    (sectorialJointAgreement) =>
      sectorialJointAgreement.agreementApplicationPeriods
  )
  @JoinColumn([
    { name: "theAgreement", referencedColumnName: "sectorialJointAgreementId" },
  ])
  theAgreement!: SectorialJointAgreement;

  @OneToMany(
    () => SalaryTable,
    (salaryTable) => salaryTable.agreementApplicationPeriods
  )
  salaryTables!: SalaryTable[];

}
