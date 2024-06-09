import { Entity, PrimaryGeneratedColumn, Column, ManyToOne, OneToMany } from 'typeorm';
import { Category } from './Category';
import { SalaryMatrix } from './SalaryMatrix'; // Add this line

@Entity()
export class Echelon {
  @PrimaryGeneratedColumn()
  id!: number;

  @Column()
  number: number=0;

  @Column()
  yearsToChange: number=0;

  @ManyToOne(() => Category, category => category.echelons)
  category: Category|undefined;

  @OneToMany(() => SalaryMatrix, salaryMatrix => salaryMatrix.echelon) // Add this line
  salaryMatrices: SalaryMatrix[]=[]; // Add this line
}