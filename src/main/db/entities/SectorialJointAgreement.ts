import { Column, Entity, OneToMany, PrimaryGeneratedColumn } from "typeorm";
import { SalaryTable } from "./SalaryTable";

@Entity("SectorialJointAgreement")
export class SectorialJointAgreement {
  @PrimaryGeneratedColumn({
    type: "integer",
    name: "sectorial_joint_AgreementID",
  })
  sectorialJointAgreementId!: number;

  @Column("text", { name: "agreement_name" })
  agreementName!: string;

  @Column("text", { name: "description", nullable: true })
  description!: string | null;

  @OneToMany(() => SalaryTable, (salaryTable) => salaryTable.agreement)
  salaryTables!: SalaryTable[];
}
