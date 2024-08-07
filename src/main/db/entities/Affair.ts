import { Column, Entity, OneToMany, PrimaryGeneratedColumn } from "typeorm";
import { ActualMonthSalary } from "./ActualMonthSalary";

@Entity("Affair")
export class Affair {
  @PrimaryGeneratedColumn({ type: "integer", name: "affairID" })
  affairId!: number;

  @Column("text", { name: "claimant" })
  claimant!: string;

  @Column("date", { name: "startDateOfWork" })
  startDateOfWork!: string;

  @Column("date", { name: "endDateOfWork" })
  endDateOfWork!: string;

  @Column("text", { name: "professionalCategoryAtBegining" })
  professionalCategoryAtBegining!: string;

  @Column("integer", { name: "professionalDegreeAtBegining" })
  professionalDegreeAtBegining!: number;

  @Column("integer", { name: "sectorial_joint_AgreementID" })
  sectorialJointAgreementId!: number;

  @OneToMany(
    () => ActualMonthSalary,
    (actualMonthSalary) => actualMonthSalary.affair
  )
  actualMonthSalaries!: ActualMonthSalary[];
}
