// src\main\db\entities\Affair.ts
import { Column, Entity, JoinColumn, ManyToOne, OneToMany, PrimaryGeneratedColumn } from "typeorm";
import { ActualMonthSalary } from "./ActualMonthSalary";
import { SectorialJointAgreement } from "./SectorialJointAgreement";
import sectorialJointAgreementSlice from "../../../renderer/redux/sectorialJointAgreement/sectorialJointAgreementSlice";

@Entity("Affair")
export class Affair {
  @PrimaryGeneratedColumn({ type: "integer", name: "affairID" })
  affairId!: number;

  @Column({ type: "text", name: "affairNumber", nullable: false })
  affairNumber!: string;

  @Column("text", { name: "title", nullable: false })
  title!: string;

  @Column("text", { name: "claimant", nullable: false })
  claimant!: string;

  @Column("date", { name: "startDateOfWork", nullable: false })
  startDateOfWork!: string;

  @Column("date", { name: "endDateOfWork", nullable: false })
  endDateOfWork!: string;

  @Column("text", { name: "professionalCategoryAtBegining" })
  professionalCategoryAtBegining!: string;

  @Column("integer", { name: "professionalDegreeAtBegining" })
  professionalDegreeAtBegining!: number;

  @ManyToOne(
    () => SectorialJointAgreement,
    (sectorialJointAgreement) => sectorialJointAgreement.affairs,
    { onDelete: 'CASCADE'}
  )
  @JoinColumn([
    { name: "agreementID" },
  ])
  agreement!: SectorialJointAgreement;

 

  @OneToMany(
    () => ActualMonthSalary,
    (actualMonthSalary) => actualMonthSalary.affair
  )
  actualMonthSalaries!: ActualMonthSalary[];
}
