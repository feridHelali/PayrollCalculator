import {
  Column,
  Entity,
  JoinColumn,
  ManyToOne,
  PrimaryGeneratedColumn,
} from "typeorm";
import { Affair } from "./Affair";

@Entity("ActualMonthSalary")
export class ActualMonthSalary {
  @PrimaryGeneratedColumn({
    type: "integer",
    name: "actualMonthSalaryID",
  })
  actualMonthSalaryId!: number;

  @Column("text", { name: "salaryPeriod" })
  salaryPeriod!: string;

  @Column("text", { name: "currentProfessionalCategory" })
  currentProfessionalCategory!: string;

  @Column("integer", { name: "currentProfessionalDegree" })
  currentProfessionalDegree!: number;

  @Column("integer", { name: "actualWorkingAge" })
  actualWorkingAge!: number;

  @Column("real", { name: "theSalary" })
  theSalary!: number;

  @ManyToOne(() => Affair, (affair) => affair.actualMonthSalaries)
  @JoinColumn([{ name: "affairID", referencedColumnName: "affairId" }])
  affair!: Affair;
}
