import { Column, Entity, OneToMany, PrimaryGeneratedColumn } from "typeorm";
import { AgreementApplicationPeriods } from "./AgreementApplicationPeriods";

@Entity("SectorialJointAgreement")
export class SectorialJointAgreement {
  @PrimaryGeneratedColumn({
    type: "integer",
    name: "sectorial_joint_AgreementID"
  })
  sectorialJointAgreementId!: number;

  @Column("text", { name: "agreement_name" })
  agreementName: string;

  @Column("text", { name: "description", nullable: true })
  description: string | null;

  @OneToMany(
    () => AgreementApplicationPeriods,
    (agreementApplicationPeriods) => agreementApplicationPeriods.theAgreement
  )
  agreementApplicationPeriods: AgreementApplicationPeriods[]=[];

  constructor(
    agreementName: string,
    description: string | null
  ) {
    this.agreementName = agreementName;
    this.description = description;
  }
}
