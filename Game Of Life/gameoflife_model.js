/**
 * This class represents a 2d array. 
 */
class My2DArray
{
    constructor (firstaccess, secondaccess)
    {
        if(firstaccess <=0 || secondaccess <=0)
        throw new Error("My2DArray must be larger than zero. ");
        this._Arr = new Array(firstaccess * secondaccess);
        this._FirstAccess = firstaccess;
        this._SecondAcess = secondaccess;
    }

    /**
     * get element of the array at that position, ondex beyond the 
     * - length of the element will loop back to the beginning
     * - negative numbers will loop back from the back. 
     * @param {*} x 
     * @param {*} y 
     */
    get(x, y)
    {
        let l = this._remapIndex(x,y); 
        return this._Arr[l[0]*this._FirstAccess+l[1]]; 
    }

    /**
     * This function remaps indices that are invalid to the size of the 
     * matrix. 
     * @param {*} x 
     * @param {*} y 
     */
    _remapIndex(x,y)
    {
        if(x<0||y<0)
        {
            if(x<0)
            {
                x =-x;
            }
            y =-y;
        }

        if(x >= this._FirstAccess || y >= this._SecondAcess)
        {
            if(x>= this._FirstAccess)
            {
                x%=this._FirstAccess;
            }
            y%=this._SecondAcess;
        }
        return [x,y]; 
    }


    /**
     * - 
     * - length of the element will loop back to the beginning
     * - negative numbers will loop back from the back. 
     * @param {*} x 
     * @param {*} y 
     * @param {*} val 
     */
    set(x, y, val)
    {
        let l = this._remapIndex(x,y);
        this._Arr[l[0]*this._FirstAccess+l[1]] = val; 
    }

    getHeight()
    {
        return this._FirstAccess;
    }

    getWidth()
    {
        return this._SecondAcess;
    }
    /**
     * Return an instance of My2DArray where each entry is either a 1 or a 0. 
     */
    static getRandomBool2DArray(x, y)
    {
        let res = new My2DArray(x,y);
        for(let i = 0; i < x; i++)for(let j = 0; j < y; j++)
        {
            res.set(i ,j, Math.floor(Math.random()*2)); 
        }
        return res; 
    }

}



/**
 * It models the game of life. 
 */
class GameOfLifeLogic
{
    

    /**
     * Pass in a 2d array to for the model of the game. 
     * @param {My2DArray} array 
     */
    constructor(array)
    {
        this.CurrentFrame = array;  // The current frame we are looking at.
        this._H = array.getWidth(); 
        this._W = array.getHeight();
        this._Tensor = new Array(); // Stores all the board in sequence. 
        print("GameOfLifeLogic constructor: ");
        print(this);
    }




    /**
     * The height of the size of the canvas
     */
    getHeight()
    {
        return this.CurrentFrame.getHeight();
    }

    /**
     * The width of the model.
     */
    getWidth()
    {
        return this.CurrentFrame.getWidth();
    }

  
    /**
     * Return the number of neigbours that are alived in a certain position.
     * * Counts the numbers of alives at that surrounded the grid at 
     * * that position. 
     */
    alive_count(x, y)
    {
        let res = 0;
        for(let i = -1; i < 2; i++)
        for(let j = -1; j < 2; j++)
        {
            res += this.CurrentFrame.get(x + i, y + j);
        }
        return res; 
    }

    /**
     * This function return a bool to indicated if the block at 
     * that position should be updated. 
     * 
     */
    should_live(posi1, posi2)
    {
        let alivecount = this.alive_count(posi1, posi2);
        if(this.CurrentFrame.get(posi1, posi2))
        {
            if(alivecount <= 3)
            {
                if(alivecount < 2)return false;
                return true;
            }
            return false;
        }
        return alivecount === 3;
    }

    /**
     * This function push the frames of the game model in the list. It does the 
     * following things: 
     * *1. Render then pushes frames into the tensor. 
     * *2. All the frames are in reverse order. 
     * 
     * @param {Integer} frames 
     * @returns {Array} 
     * An array of my2darray containing all the frames needs to plot the shit. 
     */
    update(frames = 1)
    {
        for(let i = 0; i < frames; i++)
        {
            this._updateOneFrame();
        }
        //all new rendered frames. 
        let allframes = new Array();
        for
        (
            let i = this._Tensor.length -1 , j =0;
            j < frames;
            i--, j++
        )
        {
            allframes.push(this._Tensor[i]);
        }
        return allframes; 
    }

    /**
     * Retrive the most recent matrix and then update the matrix. 
     *  * it will pushes the new frame into the current model array. 
     *  * it will refer the current frame to the newly redered frame. 
     */
    _updateOneFrame()
    {
        let newframe = new My2DArray(this._H, this._W);
        for(let i =0; i < this._W; i++)
        for(let j =0; j < this._H; j++)
        {
            newframe.set(i,j,this.should_live(i,j));
        }
        this.CurrentFrame = newframe; 
        print(this.CurrentFrame);
        this._Tensor.push(newframe);
    }
    

}
