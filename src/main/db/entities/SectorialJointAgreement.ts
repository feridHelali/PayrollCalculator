import { Column, Entity, OneToMany, PrimaryColumn, PrimaryGeneratedColumn } from "typeorm";
import { SalaryTable } from "./SalaryTable";
import { Affair } from "./Affair";

@Entity("SectorialJointAgreement")
export class SectorialJointAgreement {
  @PrimaryGeneratedColumn({
    type: "integer",
    name: "sectorial_joint_AgreementID",
  })
  sectorialJointAgreementId!: number;

  @Column("text", { name: "agreement_name", nullable: false })
  agreementName!: string;

  @Column("text", { name: "description", nullable: true })
  description!: string | null;

  @OneToMany(() => SalaryTable, (salaryTable) => salaryTable.agreement, {
    cascade: true,
    eager: true
  })
  salaryTables!: SalaryTable[];

  @OneToMany(() => Affair, (affair) => affair.agreement)
  affairs!: Affair[];
}
