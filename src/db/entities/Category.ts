import { Entity, PrimaryGeneratedColumn, Column, ManyToOne, OneToMany } from 'typeorm';
import { ConventionCollective } from './ConventionCollective';
import { Echelon } from './Echelon';
import { SalaryMatrix } from './SalaryMatrix'; // Add this line

@Entity()
export class Category {
  @PrimaryGeneratedColumn()
  id!: number;

  @Column()
  name: string="";

  @ManyToOne(() => ConventionCollective, conventionCollective => conventionCollective.categories)
  conventionCollective: ConventionCollective|undefined;

  @OneToMany(() => Echelon, echelon => echelon.category)
  echelons: Echelon[]=[];

  @OneToMany(() => SalaryMatrix, salaryMatrix => salaryMatrix.category) // Add this line
  salaryMatrices: SalaryMatrix[]=[]; // Add this line
}